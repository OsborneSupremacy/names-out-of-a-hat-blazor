using Amazon.Lambda.SQSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace GiftExchange.Library.Services;

internal class InvitationQueueHandlerService
{
    private readonly IAmazonSimpleEmailService _sesClient;

    private readonly JsonService _jsonService;

    private const string SenderEmail = "donotreply@mail.namesoutofahat.com";

    private const string TestRecipient = "osborne.ben@gmail.com";

    private readonly bool _liveMode;

    /// <summary>
    /// The SES configuration set that publishes what happens to this message.
    /// </summary>
    /// <remarks>
    /// Naming it on the send is the entire subscription: without it SES sends the mail and reports
    /// nothing, which is how this application ran until delivery tracking existed — a bounced
    /// invitation and a delivered one looked exactly alike.
    ///
    /// Read once, at construction, and empty is allowed. An environment that has not been given the
    /// name still sends mail; it just goes back to reporting nothing, rather than failing every
    /// invitation over a missing variable.
    /// </remarks>
    private readonly string _configurationSet;

    public InvitationQueueHandlerService(
        IAmazonSimpleEmailService sesClient,
        JsonService jsonService
        )
    {
        _sesClient = sesClient;
        _jsonService = jsonService;
        _liveMode = EnvReader.TryGetBooleanValue("LIVE_MODE", out var boolOut) && boolOut;
        _configurationSet = EnvReader.TryGetStringValue("SES_CONFIGURATION_SET", out var configurationSet)
            ? configurationSet ?? string.Empty
            : string.Empty;
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
            },
            // Tagged even in test mode. The events are about a real message that really was sent,
            // and recording them against the participant it was meant for is what makes the whole
            // path testable without live addresses.
            Tags =
            [
                new MessageTag { Name = SesMessageTags.ParticipantId, Value = invitation.ParticipantId.ToString() },
                new MessageTag { Name = SesMessageTags.MessageType, Value = invitation.MessageType }
            ]
        };

        // Set rather than initialized, because the property may not be set at all: SES rejects an
        // empty configuration set name outright, so an unconfigured environment has to send the
        // request without the field rather than with a blank one.
        if (!string.IsNullOrWhiteSpace(_configurationSet))
            sendRequest.ConfigurationSetName = _configurationSet;

        var response = await _sesClient
            .SendEmailAsync(sendRequest)
            .ConfigureAwait(false);

        context.Logger.LogInformation($"Email sent to {invitation.RecipientEmail}. MessageId: {response.MessageId}");
    }
}
