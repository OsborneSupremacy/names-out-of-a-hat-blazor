using Amazon.SQS.Model;
using dotenv.net.Utilities;

namespace GiftExchange.Library.Handlers;

public class SendInvitations
{
    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public SendInvitations()
    {
        _dynamoDbService = new DynamoDbService();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler.FunctionHandler<SendInvitationsRequest>
        (
            request,
            InnerHandler,
            context
        );

    private async Task<Result> InnerHandler(SendInvitationsRequest request)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        if(!hat.RecipientsAssigned)
            return new Result(new AggregateException("Recipients have not yet been assigned."), HttpStatusCode.BadRequest);

        var participantsOut = new List<Participant>();

        var sqsClient = new Amazon.SQS.AmazonSQSClient();
        var queueUrl = EnvReader.GetStringValue("INVITATIONS_QUEUE_URL");

        var messageGroupId = $"group-hat-{hat.Id}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}";

        var sqsTasks = new List<Task>();

        foreach(var participant in hat.Participants)
        {
            var invitation = new ParticipantInvitationRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                HtmlBody = EmailCompositionService
                    .ComposeEmail(hat, participant.Person.Name, participant.PickedRecipient.Name),
                RecipientEmail = participant.Person.Email,
                Subject = EmailCompositionService.GetSubject(hat)
            };

            var jsonInvitation = JsonService.SerializeDefault(invitation);

            var sqsRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = jsonInvitation,
                MessageGroupId = messageGroupId
            };

            sqsTasks.Add(sqsClient.SendMessageAsync(sqsRequest));
        }

        await Task.WhenAll(sqsTasks)
            .ConfigureAwait(false);

        return new Result(HttpStatusCode.OK);
    }
}
