using Amazon.SQS;
using Amazon.SQS.Model;

namespace GiftExchange.Library.Services;

/// <summary>
/// Puts an outgoing participant email onto the queue the sending function reads.
/// </summary>
/// <remarks>
/// Both of the things this application sends to everybody at once — the invitations, and the
/// message saying the exchange has finished — go through here, so the queue is named in one place
/// and a caller only has to build the message.
///
/// Distinct from <see cref="AutomaticEmailSender"/>, which sends immediately, and is for a message
/// answering something one person just did. This is for a fan-out: a hat with thirty participants
/// is thirty sends, and doing them inside the organizer's own request would spend that request's
/// budget on SES rather than on the write the organizer is waiting for.
/// </remarks>
[UsedImplicitly]
internal class EmailQueue : IEmailQueue
{
    private readonly IAmazonSQS _sqsClient;

    private readonly JsonService _jsonService;

    private readonly string _queueUrl;

    public EmailQueue(IAmazonSQS sqsClient, JsonService jsonService)
    {
        _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
        _queueUrl = EnvReader.GetStringValue("INVITATIONS_QUEUE_URL");
    }

    public Task EnqueueAsync(GiftExchangeEmailRequest email) =>
        _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = _jsonService.SerializeDefault(email)
        });
}
