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

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public InvitationQueueHandler()
    {
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

        context.Logger.LogInformation(
            $"Sending email to {invitation.RecipientEmail} with subject '{invitation.Subject}'");

        var recipient = _liveMode ? invitation.RecipientEmail : TestRecipient;

        var sendRequest = new SendEmailRequest
        {
            Source = SenderEmail,
            Destination = new Destination { ToAddresses = [recipient] },
            Message = new Message
            {
                Subject = new Content(invitation.Subject),
                Body = new Body { Html = new Content(invitation.HtmlBody) }
            }
        };

        var response = await _sesClient
            .SendEmailAsync(sendRequest)
            .ConfigureAwait(false);

        context.Logger.LogInformation($"Email sent to {invitation.RecipientEmail}. MessageId: {response.MessageId}");
    }
}

