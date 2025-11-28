using Amazon.SQS;
using Amazon.SQS.Model;

namespace GiftExchange.Library.Services;

[UsedImplicitly]
public class EnqueueInvitationsService : IBusinessService<SendInvitationsRequest, StatusCodeOnlyResponse>
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly JsonService _jsonService;

    private readonly EmailCompositionService _emailCompositionService;

    private readonly IAmazonSQS _sqsClient;

    public EnqueueInvitationsService(
        GiftExchangeProvider giftExchangeProvider,
        JsonService jsonService,
        EmailCompositionService emailCompositionService,
        IAmazonSQS sqsClient
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
        _emailCompositionService =
            emailCompositionService ?? throw new ArgumentNullException(nameof(emailCompositionService));
        _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(SendInvitationsRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound);

        if (!hat.RecipientsAssigned)
            return new Result<StatusCodeOnlyResponse>(new AggregateException("Recipients have not yet been assigned."),
                HttpStatusCode.BadRequest);

        var queueUrl = EnvReader.GetStringValue("INVITATIONS_QUEUE_URL");

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
                QueueUrl = queueUrl,
                MessageBody = jsonInvitation,
                MessageGroupId = messageGroupId
            };

            sqsTasks.Add(_sqsClient.SendMessageAsync(sqsRequest));
        }

        await Task.WhenAll(sqsTasks)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
