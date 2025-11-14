using Amazon.Lambda.SQSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace GiftExchange.Library.Services;

internal class InvitationQueueHandlerService
{
    private const string SenderEmail = "namesoutofahat@osbornesupremacy.com";

    private const string TestRecipient = "osborne.ben@gmail.com";

    private readonly bool _liveMode;

    private readonly IAmazonSimpleEmailService _sesClient;


}
