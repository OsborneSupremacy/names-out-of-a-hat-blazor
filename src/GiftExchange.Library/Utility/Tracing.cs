using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Sampling;

namespace GiftExchange.Library.Utility;

/// <summary>
/// The application's own additions to what X-Ray records automatically.
/// </summary>
/// <remarks>
/// The AWS SDK pipeline handler and the EF interceptor registered in
/// <see cref="Builders.ServiceProviderBuilder"/> cover every call that leaves the process. This
/// covers the three things they cannot: work that is expensive but local, the labels that make one
/// trace findable among thousands, and carrying a trace across a queue.
///
/// Every member here does nothing at all when there is no active trace, which is the normal state
/// outside Lambda. That is deliberate and it is what keeps instrumentation from being something
/// tests and local runs have to work around: the calls stay in the code path, and off Lambda they
/// cost a null check. Nothing here ever throws for a tracing reason — an observability tool that
/// can fail a request is worse than one that records nothing.
/// </remarks>
internal static class Tracing
{
    private static bool IsTracing
    {
        get
        {
            try
            {
                return AWSXRayRecorder.Instance.TraceContext.IsEntityPresent();
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Runs <paramref name="work"/> inside a named subsegment, so its share of the invocation is
    /// visible in the trace rather than folded into the caller's time.
    /// </summary>
    /// <remarks>
    /// For work that never leaves the process and is therefore invisible to every automatic
    /// handler — building the EF model, opening the first pooled DSQL connection, signing the IAM
    /// token that opening it requires. Those three are named in the memory_size comment on the
    /// application Lambda as the reason a cold start was breaching the API Gateway ceiling, and
    /// until now the split between them has only ever been guessed at.
    /// </remarks>
    public static T Measure<T>(string name, Func<T> work)
    {
        if (!IsTracing) return work();

        AWSXRayRecorder.Instance.BeginSubsegment(name);
        try
        {
            return work();
        }
        catch (Exception e)
        {
            AWSXRayRecorder.Instance.AddException(e);
            throw;
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }
    }

    /// <summary>
    /// Attaches an indexed, filterable label to the current trace.
    /// </summary>
    /// <remarks>
    /// Annotations are indexed, which is the entire point — they are what turns "show me a trace"
    /// into "show me every trace for this endpoint that was slow". It is also the reason nothing
    /// identifying a person may be passed here. An email address in an annotation is materially
    /// worse than the same address in a log line: the log line has a retention period and has to
    /// be searched for deliberately, while an annotation is indexed for querying by design.
    ///
    /// Ids, counts, statuses and route keys. Anything about a participant belongs nowhere near it.
    /// </remarks>
    public static void Annotate(string key, string value)
    {
        if (!IsTracing) return;

        try
        {
            AWSXRayRecorder.Instance.AddAnnotation(key, value);
        }
        catch
        {
            // A malformed key, or a race with the segment ending. Neither is worth a failed
            // request, and neither is worth a log line on every invocation that hits it.
        }
    }

    /// <summary>
    /// The current trace context formatted as an <c>AWSTraceHeader</c> value, or null when there
    /// is nothing to propagate.
    /// </summary>
    /// <remarks>
    /// The X-Ray SDK instruments the SendMessage call but does not put anything on the message, so
    /// without this a trace stops at the queue: the organizer's request ends at "SQS accepted it"
    /// and the send that follows is an unrelated trace with no way back to the click that caused
    /// it. Set as a message system attribute, Lambda's event source mapping reads this and
    /// continues the trace into the queue handler, which makes the whole journey — click, queue,
    /// send, SES — one thing to look at.
    /// </remarks>
    public static string? CurrentTraceHeader
    {
        get
        {
            if (!IsTracing) return null;

            try
            {
                var entity = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                var sampled = entity.Sampled == SampleDecision.Sampled ? "1" : "0";
                return $"Root={entity.RootSegment.TraceId};Parent={entity.Id};Sampled={sampled}";
            }
            catch
            {
                return null;
            }
        }
    }
}
