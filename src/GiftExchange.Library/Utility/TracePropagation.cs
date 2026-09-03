using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Sampling;

namespace GiftExchange.Library.Utility;

/// <summary>
/// Carries the current trace across a queue, which is the one piece of tracing neither Powertools
/// nor the X-Ray SDK does on its own.
/// </summary>
/// <remarks>
/// Everything else this application needed instrumentation for now comes from
/// <c>AWS.Lambda.Powertools.Tracing</c>: the <c>[Tracing]</c> attribute on each handler, subsegments
/// around expensive local work, and annotations. This is what remains.
///
/// The X-Ray SDK instruments the SendMessage call itself but puts nothing on the message, so
/// without this a trace stops at the queue -- the organizer's request ends at "SQS accepted it",
/// and the send that follows is an unrelated trace with no way back to the click that caused it.
/// With the header set as a message system attribute, Lambda's event source mapping continues the
/// trace into the queue handler and the whole journey becomes one thing to look at.
///
/// Returns null rather than throwing whenever there is no trace to propagate, which is the normal
/// state outside Lambda. An observability tool that can fail a request is worse than one that
/// records nothing.
/// </remarks>
internal static class TracePropagation
{
    /// <summary>
    /// The current trace context formatted as an <c>AWSTraceHeader</c> value, or null when there
    /// is nothing to propagate.
    /// </summary>
    public static string? CurrentTraceHeader
    {
        get
        {
            try
            {
                if (!AWSXRayRecorder.Instance.TraceContext.IsEntityPresent()) return null;

                var entity = AWSXRayRecorder.Instance.TraceContext.GetEntity();
                var sampled = entity.Sampled == SampleDecision.Sampled ? "1" : "0";

                // Parent is the current entity rather than the root segment, so the queue
                // handler's trace attaches to the enqueue specifically rather than to the request
                // in general -- which is the difference between seeing that a request sent
                // something and seeing which send took four minutes.
                return $"Root={entity.RootSegment.TraceId};Parent={entity.Id};Sampled={sampled}";
            }
            catch
            {
                return null;
            }
        }
    }
}
