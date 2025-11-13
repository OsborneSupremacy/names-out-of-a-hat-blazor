using Amazon.Lambda.SQSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using dotenv.net.Utilities;

namespace GiftExchange.Library.Handlers;

[UsedImplicitly]
public class InvitationQueueHandler
{
    private const string SenderEmail = "namesofahat@osbornesupremacy.com";

    private const string TestRecipient = "osborne.ben@gmail.com";

    private readonly bool _liveMode;

    private readonly IAmazonSimpleEmailService _sesClient;

    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public InvitationQueueHandler()
    {
        _dynamoDbService = new DynamoDbService();
        _sesClient = new AmazonSimpleEmailServiceClient(
            Amazon.RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION")!)
        );
        _liveMode = EnvReader.TryGetBooleanValue("LIVE_MODE", out var boolOut) && boolOut;
    }

    // ReSharper disable once UnusedMember.Global
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var record in evnt.Records)
            await ProcessRecordAsync(record, context)
                .ConfigureAwait(false);
    }

    private async Task ProcessRecordAsync(SQSEvent.SQSMessage record, ILambdaContext context)
    {
        var invitation = JsonService.DeserializeDefault<ParticipantInvitationRequest>(record.Body);

        if (invitation is null)
            throw new AggregateException($"Invalid message body: {record.Body}");

        context.Logger.LogInformation($"Sending email to {invitation.RecipientEmail} with subject '{invitation.Subject}'");

        var recipient = _liveMode ? invitation.RecipientEmail : TestRecipient;

        var sendRequest = new SendEmailRequest
        {
            Source = SenderEmail,
            Destination = new Destination
            {
                ToAddresses = [ recipient ]
            },
            Message = new Message
            {
                Subject = new Content(invitation.Subject),
                Body = new Body
                {
                    Html = new Content(invitation.HtmlBody)
                }
            }
        };

        var response = await _sesClient
            .SendEmailAsync(sendRequest)
            .ConfigureAwait(false);

        context.Logger.LogInformation($"Email sent to {invitation.RecipientEmail}. MessageId: {response.MessageId}");

        try
        {
            await MarkEmailAsSentAsync(invitation, context);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "An error was encountered when trying to mark the invitation as sent for the participant. The email was already sent, so this error will not stop the function from completing successfully.");
        }
    }

    private async Task MarkEmailAsSentAsync(ParticipantInvitationRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if (!hatExists)
            throw new AggregateException($"Hat {request.OrganizerEmail}, ID {request.HatId} not found.");

        var participantsIn = hat.Participants.ToList();

        var participant = participantsIn
            .FirstOrDefault(p => p.Person.Email.ContentEquals(request.RecipientEmail));

        if (participant is null)
            throw new AggregateException($"Participant {request.RecipientEmail} not found in Hat {request.OrganizerEmail}, ID {request.HatId} not found.");

        participantsIn.Remove(participant);

        var participantsOut = participantsIn
            .Concat([participant with { InvitationSent = true }])
            .ToImmutableList();

        await _dynamoDbService
            .UpdateParticipantsAsync(request.OrganizerEmail, request.HatId, participantsOut)
            .ConfigureAwait(false);
    }
}

