using Amazon.SQS;
using Amazon.SQS.Model;

namespace GiftExchange.Library.Services;

[UsedImplicitly]
internal class EnqueueInvitationsService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly ApiGatewayAdapter _adapter;

    private readonly JsonService _jsonService;

    private readonly EmailCompositionService _emailCompositionService;

    private readonly IAmazonSQS _sqsClient;

    private readonly string _queueUrl;

    public EnqueueInvitationsService(
        GiftExchangeProvider giftExchangeProvider,
        ApiGatewayAdapter adapter,
        JsonService jsonService,
        EmailCompositionService emailCompositionService,
        IAmazonSQS sqsClient
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
        _emailCompositionService =
            emailCompositionService ?? throw new ArgumentNullException(nameof(emailCompositionService));
        _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
        _queueUrl = EnvReader.GetStringValue("INVITATIONS_QUEUE_URL");
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context) =>
        _adapter.AdaptAsync<SendInvitationsRequest, StatusCodeOnlyResponse>(request, ExecuteAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(SendInvitationsRequest request)
    {
        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound);

        if (!hat.RecipientsAssigned)
            return new Result<StatusCodeOnlyResponse>(new AggregateException("Recipients have not yet been assigned."),
                HttpStatusCode.BadRequest);

        if(hat.InvitationsQueued)
            return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);

        var messageGroupId = $"group-hat-{hat.Id}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}";

        var sqsTasks = new List<Task>();

        foreach(var participant in hat.Participants)
        {
            var invitation = new ParticipantInvitationRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                HtmlBody = _emailCompositionService
                    .ComposeEmail(hat, participant.Person.Name, participant.PickedRecipient),
                RecipientEmail = participant.Person.Email,
                Subject = EmailCompositionService.GetSubject(hat)
            };

            var jsonInvitation = _jsonService.SerializeDefault(invitation);

            var sqsRequest = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = jsonInvitation,
                MessageGroupId = messageGroupId
            };

            sqsTasks.Add(_sqsClient.SendMessageAsync(sqsRequest));
        }

        await Task.WhenAll(sqsTasks)
            .ConfigureAwait(false);

        await _giftExchangeProvider
            .MarkInvitationsAsQueuedAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
