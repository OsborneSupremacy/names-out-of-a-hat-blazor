using Amazon.S3;

// The SES event library nests its record and receipt types inside the event and makes each of them
// generic over the same action type again, so spelled out in full they are unreadable. Aliased once
// here rather than repeated at every signature.
using InboundRecord = Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<
    Amazon.Lambda.SimpleEmailEvents.Actions.LambdaReceiptAction>.SimpleEmailRecord<
    Amazon.Lambda.SimpleEmailEvents.Actions.LambdaReceiptAction>;
using InboundReceipt = Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<
    Amazon.Lambda.SimpleEmailEvents.Actions.LambdaReceiptAction>.SimpleEmailReceipt<
    Amazon.Lambda.SimpleEmailEvents.Actions.LambdaReceiptAction>;

namespace GiftExchange.Library.Services;

/// <summary>
/// Handles one message arriving at a gift ideas address.
/// </summary>
/// <remarks>
/// The order of the checks below is the design. Everything that decides whether we are willing to
/// speak to this sender at all comes first, and every one of those failures ends in silence: at
/// that point nothing has established who wrote in, and answering an address we cannot vouch for is
/// what turns a mailbox into a way of sending mail to strangers. Only once a live token and a
/// matching From have both been seen is there a known participant to reply to, and from there every
/// refusal says why.
/// </remarks>
[UsedImplicitly]
internal class InboundGiftIdeasService
{
    /// <summary>The address every outbound message comes from, and one this rule also answers.</summary>
    private const string SenderEmail = "donotreply@mail.namesoutofahat.com";

    private const string GiftIdeasDomain = "ideas.namesoutofahat.com";

    /// <summary>Statuses during which there is still somebody to share ideas with.</summary>
    private static readonly ImmutableList<string> AcceptingStatuses =
        [HatStatus.NamesAssigned, HatStatus.InvitationsSent, HatStatus.CooledOff];

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly IAmazonS3 _s3Client;

    private readonly AutomaticEmailSender _sender;

    private readonly IContentModerationService _contentModerationService;

    private readonly InboundEmailParser _parser;

    private readonly GiftIdeaContentPolicy _contentPolicy;

    private readonly GiftIdeaEmailCompositionService _composer;

    private readonly IReplyThrottleProvider _replyThrottleProvider;

    private readonly ILogger<InboundGiftIdeasService> _logger;

    private readonly string _bucketName;

    private readonly string _objectKeyPrefix;

    public InboundGiftIdeasService(
        GiftExchangeProvider giftExchangeProvider,
        IAmazonS3 s3Client,
        AutomaticEmailSender sender,
        IContentModerationService contentModerationService,
        InboundEmailParser parser,
        GiftIdeaContentPolicy contentPolicy,
        GiftIdeaEmailCompositionService composer,
        IReplyThrottleProvider replyThrottleProvider,
        ILogger<InboundGiftIdeasService> logger
    )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _contentModerationService = contentModerationService ?? throw new ArgumentNullException(nameof(contentModerationService));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _contentPolicy = contentPolicy ?? throw new ArgumentNullException(nameof(contentPolicy));
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        _replyThrottleProvider = replyThrottleProvider ?? throw new ArgumentNullException(nameof(replyThrottleProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bucketName = EnvReader.GetStringValue("INBOUND_MAIL_BUCKET");
        _objectKeyPrefix = EnvReader.TryGetStringValue("INBOUND_MAIL_PREFIX", out var prefix) ? prefix ?? string.Empty : string.Empty;
    }

    public async Task<GiftIdeaSubmissionOutcome> ProcessRecordAsync(InboundRecord record)
    {
        var mail = record.Ses.Mail;
        var receipt = record.Ses.Receipt;

        // SES has already done the work of deciding whether this message is what it claims to be.
        // Nothing further is worth doing if it is not, and nothing is sent back either — a reply
        // here would go to whatever address a spoofer chose to put in From.
        if (!PassedAuthentication(receipt))
        {
            _logger.LogWarning(
                "Dropped an inbound message that failed authentication. MessageId: {MessageId}",
                mail.MessageId);
            return GiftIdeaSubmissionOutcome.DroppedFailedAuthentication;
        }

        var recipient = FindOurRecipient(receipt);

        if (IsDoNotReplyAddress(recipient))
            return await AnswerDoNotReplyAsync(mail.Source).ConfigureAwait(false);

        var (found, route) = await _giftExchangeProvider
            .FindGiftIdeaRouteAsync(SecretToken.Hash(ExtractToken(recipient)))
            .ConfigureAwait(false);

        // An address nobody was issued. Silence rather than a bounce: answering would confirm which
        // addresses exist, and would do it to whoever the message claims to be from.
        if (!found)
        {
            _logger.LogInformation("Dropped an inbound message addressed to an unknown gift ideas token.");
            return GiftIdeaSubmissionOutcome.DroppedUnknownToken;
        }

        await using var rawMessage = await ReadRawMessageAsync(mail.MessageId).ConfigureAwait(false);

        var email = _parser.Parse(rawMessage);

        // The token says which row to write. This says who is allowed to write it. The token
        // travels in an address that gets forwarded and quoted, so on its own it is a weaker claim
        // than it looks, and pairing it with the participant's own address is what makes it hold.
        if (!email.From.Equals(route.Sender.Email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Dropped an inbound message whose From does not match the participant the token belongs to.");
            return GiftIdeaSubmissionOutcome.DroppedSenderMismatch;
        }

        if (email.IsAutomated)
        {
            _logger.LogInformation("Dropped an automated message sent to a gift ideas address.");
            return GiftIdeaSubmissionOutcome.DroppedAutomatedMessage;
        }

        // From here the sender is known, so every exit tells them what happened.
        if (!AcceptingStatuses.Contains(route.HatStatus))
            return await ReplyWithRejectionAsync(
                    route, GiftIdeaSubmissionOutcome.RejectedExchangeNotAcceptingIdeas, email)
                .ConfigureAwait(false);

        var policyOutcome = _contentPolicy.Check(email.Body, route.SenderPickedRecipient.Name);

        if (policyOutcome != GiftIdeaSubmissionOutcome.Shared)
            return await ReplyWithRejectionAsync(route, policyOutcome, email).ConfigureAwait(false);

        var (isClean, _) = await _contentModerationService
            .ValidateContentAsync(email.Body, "gift ideas")
            .ConfigureAwait(false);

        if (!isClean)
            return await ReplyWithRejectionAsync(
                    route, GiftIdeaSubmissionOutcome.RejectedInappropriateContent, email)
                .ConfigureAwait(false);

        await _giftExchangeProvider
            .AddGiftIdeaAsync(route.ParticipantId, email.Body, mail.MessageId)
            .ConfigureAwait(false);

        // Stored before either message goes out. If sending fails after this, the submission still
        // exists and can be delivered again; the reverse would lose what somebody wrote.
        await ForwardToGiverAsync(route, email.Body).ConfigureAwait(false);

        await _sender.SendAsync(
                route.Sender.Email,
                GiftIdeaEmailCompositionService.ConfirmationSubject,
                _composer.ComposeConfirmation(email.Body, email.AttachmentNames))
            .ConfigureAwait(false);

        return GiftIdeaSubmissionOutcome.Shared;
    }

    /// <summary>
    /// Every verdict SES reports has to be a pass.
    /// </summary>
    /// <remarks>
    /// Treats anything that is not the word "PASS" as a failure, including a missing verdict. The
    /// alternative — listing the failure statuses and letting everything else through — turns any
    /// status AWS adds later into an accepted message.
    /// </remarks>
    private static bool PassedAuthentication(InboundReceipt receipt) =>
        IsPass(receipt.SpamVerdict?.Status)
        && IsPass(receipt.VirusVerdict?.Status)
        && IsPass(receipt.SPFVerdict?.Status)
        && IsPass(receipt.DKIMVerdict?.Status)
        && IsPass(receipt.DMARCVerdict?.Status);

    private static bool IsPass(string? status) =>
        "PASS".Equals(status, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Which of the addresses on the message is one of ours.
    /// </summary>
    /// <remarks>
    /// Read from the receipt rather than from To or Cc. A message can be addressed to several
    /// people, and it can reach us by Bcc and name us in neither — the receipt says which recipient
    /// SES actually matched a rule for, which is the only one we have any business acting on.
    /// </remarks>
    private static string FindOurRecipient(InboundReceipt receipt) =>
        receipt.Recipients?
            .FirstOrDefault(address =>
                address.EndsWith($"@{GiftIdeasDomain}", StringComparison.OrdinalIgnoreCase)
                || address.Equals(SenderEmail, StringComparison.OrdinalIgnoreCase))
            ?.Trim()
        ?? string.Empty;

    private static bool IsDoNotReplyAddress(string recipient) =>
        recipient.Equals(SenderEmail, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The local part of the address, which is the token.
    /// </summary>
    /// <remarks>
    /// Case is preserved, and that is not incidental. The token is base64url, so "aB" and "Ab" are
    /// different tokens, and lower-casing the address before taking it — the ordinary thing to do
    /// with an email address — turns every valid submission into an unknown one. The comparisons
    /// against our own domains are case-insensitive on their own account instead.
    /// </remarks>
    private static string ExtractToken(string recipient)
    {
        var at = recipient.IndexOf('@');
        return at <= 0 ? string.Empty : recipient[..at];
    }

    /// <summary>
    /// Tells somebody who wrote to the no-reply address that nobody read it, at most once a day.
    /// </summary>
    /// <remarks>
    /// The one reply sent to an address that is not a known participant's, which is why it is the
    /// one that needs the throttle. Its own header marks it as automatic, so another autoresponder
    /// on the far end will not answer it back.
    /// </remarks>
    private async Task<GiftIdeaSubmissionOutcome> AnswerDoNotReplyAsync(string source)
    {
        if (!await _replyThrottleProvider.TryReserveReplySlotAsync("DONOTREPLY", source).ConfigureAwait(false))
        {
            _logger.LogInformation("Suppressed a do-not-reply response inside the throttle window.");
            return GiftIdeaSubmissionOutcome.RedirectedFromDoNotReply;
        }

        await _sender.SendAsync(source, GiftIdeaEmailCompositionService.DoNotReplySubject, _composer.ComposeDoNotReply())
            .ConfigureAwait(false);

        return GiftIdeaSubmissionOutcome.RedirectedFromDoNotReply;
    }

    private async Task<GiftIdeaSubmissionOutcome> ReplyWithRejectionAsync(
        GiftIdeaRoute route,
        GiftIdeaSubmissionOutcome outcome,
        InboundEmail email
    )
    {
        _logger.LogInformation("Refused a gift ideas submission: {Outcome}", outcome);

        await _sender.SendAsync(
                route.Sender.Email,
                GiftIdeaEmailCompositionService.CouldNotShareSubject,
                _composer.ComposeRejection(outcome, email.AttachmentNames))
            .ConfigureAwait(false);

        return outcome;
    }

    private Task ForwardToGiverAsync(GiftIdeaRoute route, string ideas)
    {
        // Nobody has drawn this participant, so there is nobody to forward to. The submission is
        // already stored, and whoever draws them later can be sent it then.
        if (string.IsNullOrWhiteSpace(route.Giver.Email))
        {
            _logger.LogInformation("Stored a gift ideas submission with nobody yet to forward it to.");
            return Task.CompletedTask;
        }

        return _sender.SendAsync(
            route.Giver.Email,
            GiftIdeaEmailCompositionService.ForwardSubject(route.Sender.Name),
            _composer.ComposeForward(route.Sender.Name, route.HatName, ideas));
    }

    private async Task<Stream> ReadRawMessageAsync(string messageId)
    {
        // SES writes the object under the message id, which is the only handle the event gives us.
        var response = await _s3Client
            .GetObjectAsync(_bucketName, $"{_objectKeyPrefix}{messageId}")
            .ConfigureAwait(false);

        // Copied out so the S3 stream can be closed here rather than held open across parsing.
        var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer).ConfigureAwait(false);
        buffer.Position = 0;

        return buffer;
    }

}
