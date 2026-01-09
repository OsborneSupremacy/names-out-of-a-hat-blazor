using Amazon.Lambda.SQSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace GiftExchange.Library.Services;

internal class InvitationQueueHandlerService
{
    private readonly IAmazonSimpleEmailService _sesClient;

    private readonly JsonService _jsonService;

    private const string SenderEmail = "namesoutofahat@osbornesupremacy.com";

    private const string TestRecipient = "osborne.ben@gmail.com";

    private readonly bool _liveMode;

    public InvitationQueueHandlerService(
        IAmazonSimpleEmailService sesClient,
        JsonService jsonService
        )
    {
        _sesClient = sesClient;
        _jsonService = jsonService;
        _liveMode = EnvReader.TryGetBooleanValue("LIVE_MODE", out var boolOut) && boolOut;
    }

    public async Task ProcessRecordAsync(SQSEvent.SQSMessage record, ILambdaContext context)
    {
        var invitation = _jsonService.DeserializeDefault<GiftExchangeEmailRequest>(record.Body);

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
                Subject = new Content(invitation.Subject + (_liveMode ? string.Empty : " - TEST MODE")),
                Body = new Body { Html = new Content(invitation.HtmlBody) }
            }
        };

        var response = await _sesClient
            .SendEmailAsync(sendRequest)
            .ConfigureAwait(false);

        context.Logger.LogInformation($"Email sent to {invitation.RecipientEmail}. MessageId: {response.MessageId}");
    }
}
