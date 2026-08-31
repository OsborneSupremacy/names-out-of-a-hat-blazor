using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace GiftExchange.Library.Services;

/// <summary>
/// Takes a message from the contact form in the footer and publishes it to the feedback SNS topic,
/// which an email subscription delivers to whoever is looking after the application.
/// </summary>
/// <remarks>
/// A topic of its own, deliberately not the alarms topic. Subscriptions are per-topic, so the
/// moment alarms need to reach a pager or a remediation Lambda, that subscriber would start
/// receiving feedback too; and sharing would mean this endpoint — the one reachable by anybody
/// with an account — holding <c>sns:Publish</c> on the channel that has to stay trustworthy.
/// Topics themselves are free, so there is nothing to be saved by sharing one.
/// </remarks>
[UsedImplicitly]
internal class SubmitFeedbackService : IApiGatewayHandler
{
    private readonly ILogger<SubmitFeedbackService> _logger;

    private readonly ApiGatewayAdapter _adapter;

    private readonly IAmazonSimpleNotificationService _snsClient;

    private readonly string _topicArn;

    public SubmitFeedbackService(
        ILogger<SubmitFeedbackService> logger,
        ApiGatewayAdapter adapter,
        IAmazonSimpleNotificationService snsClient
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _snsClient = snsClient ?? throw new ArgumentNullException(nameof(snsClient));
        _topicArn = EnvReader.GetStringValue("FEEDBACK_TOPIC_ARN");
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context) =>
        _adapter.AdaptAsync<SubmitFeedbackRequest, StatusCodeOnlyResponse>(request, ExecuteAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(SubmitFeedbackRequest request)
    {
        var publishRequest = new PublishRequest
        {
            TopicArn = _topicArn,
            Subject = BuildSubject(request.Category),
            Message = BuildBody(request),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                // Not read by anything today. It is here so that splitting the categories across
                // subscriptions later is a filter policy rather than a new topic and a deploy.
                ["category"] = new() { DataType = "String", StringValue = request.Category }
            }
        };

        try
        {
            await _snsClient.PublishAsync(publishRequest).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Reported to the sender, unlike the magic link endpoint's failures. There, silence is
            // the point — the caller must not learn whether an address exists. Here the sender has
            // just typed something they want read, and letting the form say "thanks" when nothing
            // was published means they never send it again by another route.
            _logger.LogError(exception, "Failed to publish feedback to the topic.");

            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(
                    "We couldn't send that just now. Please try again, or email giftexchange@osbornesupremacy.com directly."),
                HttpStatusCode.BadGateway);
        }

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.Accepted },
            HttpStatusCode.Accepted);
    }

    /// <summary>
    /// The category and nothing else.
    /// </summary>
    /// <remarks>
    /// An SNS subject is capped at 100 characters and must be printable ASCII with no line breaks;
    /// a publish carrying anything else is rejected outright. Putting the sender's address here
    /// would look better in an inbox and would hand that rejection to whichever organizer first
    /// signs up with a non-ASCII address. The body has room for it and no such rules.
    /// </remarks>
    private static string BuildSubject(string category) =>
        $"[Names Out Of A Hat] {FeedbackCategories.Describe(category)}";

    private static string BuildBody(SubmitFeedbackRequest request) =>
        $"""
         {FeedbackCategories.Describe(request.Category)}

         From:      {request.OrganizerEmail}
         Received:  {DateTimeOffset.UtcNow:u}

         ----------------------------------------------------------------------

         {request.Message}
         """;
}
