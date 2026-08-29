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

        // Kept rather than only hashed: a refusal has to tell the sender where to send the next
        // attempt, and this address is that place.
        var token = ExtractToken(recipient);

        var (found, route) = await ResolveRouteAsync(SecretToken.Hash(token)).ConfigureAwait(false);

        // An address nobody was issued. Silence rather than a bounce: answering would confirm which
        // addresses exist, and would do it to whoever the message claims to be from.
        if (!found)
        {
            _logger.LogInformation("Dropped an inbound message addressed to an unknown gift ideas token.");
            return GiftIdeaSubmissionOutcome.DroppedUnknownToken;
        }

        await using var rawMessage = await ReadRawMessageAsync(mail.MessageId).ConfigureAwait(false);

        var email = _parser.Parse(rawMessage);

        var senderOutcome = CheckSender(email, route);

        // The last exit that ends in silence.
        if (senderOutcome != GiftIdeaSubmissionOutcome.Shared)
            return senderOutcome;

        // From here the sender is known, so every exit tells them what happened.
        var contentOutcome = await CheckContentAsync(email, route).ConfigureAwait(false);

        if (contentOutcome != GiftIdeaSubmissionOutcome.Shared)
            return await ReplyWithRejectionAsync(route, contentOutcome, email, token).ConfigureAwait(false);

        return await ShareAsync(route, email, mail.MessageId).ConfigureAwait(false);
    }

    /// <summary>
    /// Whether this message may be treated as having come from the participant its token belongs
    /// to.
    /// </summary>
    /// <remarks>
    /// The last of the checks that end in silence, and the one that decides the sender is real.
    /// Everything after it can safely write back; nothing before it can.
    /// </remarks>
    /// <returns>
    /// <see cref="GiftIdeaSubmissionOutcome.Shared"/> when there is nothing wrong, in the same
    /// sense <see cref="GiftIdeaContentPolicy.Check"/> uses it: not that anything has been shared
    /// yet, but that nothing stands in the way.
    /// </returns>
    private GiftIdeaSubmissionOutcome CheckSender(InboundEmail email, GiftIdeaRoute route)
    {
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

        return GiftIdeaSubmissionOutcome.Shared;
    }

    /// <summary>
    /// Whether there is anything about this submission that stops it being passed on.
    /// </summary>
    /// <remarks>
    /// Every outcome here is a Rejected one, which is the property that lets the caller answer all
    /// of them the same way instead of writing a reply beside each check. Whether the exchange is
    /// still running sits alongside the content rules rather than above them because from the
    /// sender's side it is the same kind of answer: a reason, and something to do about it.
    ///
    /// Ordered cheapest first, so a message refused on a rule this application can apply itself
    /// never reaches Comprehend.
    /// </remarks>
    /// <returns><see cref="GiftIdeaSubmissionOutcome.Shared"/> when nothing is wrong, as above.</returns>
    private async Task<GiftIdeaSubmissionOutcome> CheckContentAsync(InboundEmail email, GiftIdeaRoute route)
    {
        if (!AcceptingStatuses.Contains(route.HatStatus))
            return GiftIdeaSubmissionOutcome.RejectedExchangeNotAcceptingIdeas;

        var policyOutcome = _contentPolicy.Check(email.Body, route.SenderPickedRecipient.Name);

        if (policyOutcome != GiftIdeaSubmissionOutcome.Shared)
            return policyOutcome;

        var (isClean, _) = await _contentModerationService
            .ValidateContentAsync(email.Body, "gift ideas")
            .ConfigureAwait(false);

        return isClean
            ? GiftIdeaSubmissionOutcome.Shared
            : GiftIdeaSubmissionOutcome.RejectedInappropriateContent;
    }

    /// <summary>
    /// Stores the submission, sends it on, and tells the sender it went.
    /// </summary>
    /// <remarks>
    /// Stored before either message goes out. If sending fails after this, the submission still
    /// exists and can be delivered again; the reverse would lose what somebody wrote.
    /// </remarks>
    private async Task<GiftIdeaSubmissionOutcome> ShareAsync(
        GiftIdeaRoute route,
        InboundEmail email,
        string inboundMessageId
    )
    {
        await StoreAsync(route, email.Body, inboundMessageId).ConfigureAwait(false);

        await ForwardToGiverAsync(route, email.Body).ConfigureAwait(false);

        await ConfirmToSenderAsync(route, email).ConfigureAwait(false);

        return GiftIdeaSubmissionOutcome.Shared;
    }

    /// <summary>
    /// Writes the submission to whichever table it belongs in.
    /// </summary>
    /// <remarks>
    /// Two tables, because the two are not the same claim. What somebody says about themselves is
    /// theirs; what somebody says about another participant is a suggestion made to the one person
    /// who asked for it, and must never be read back as the subject's own words.
    /// </remarks>
    private Task<Guid> StoreAsync(GiftIdeaRoute route, string ideas, string inboundMessageId) =>
        route.IsContribution switch
        {
            true => _giftExchangeProvider.AddContributedGiftIdeaAsync(route.AskId, ideas, inboundMessageId),
            false => _giftExchangeProvider.AddGiftIdeaAsync(route.ParticipantId, ideas, inboundMessageId)
        };

    /// <summary>
    /// Echoes back to the sender exactly what was kept, so a bad guess at where their message ended
    /// is something they can see and correct.
    /// </summary>
    private Task ConfirmToSenderAsync(GiftIdeaRoute route, InboundEmail email)
    {
        // Subject and body chosen together rather than one ternary each, so that the two cannot
        // be made to disagree about which kind of message this is.
        var (subject, body) = route.IsContribution switch
        {
            true => (
                GiftIdeaEmailCompositionService.ContributionConfirmationSubject(route.Subject.Name),
                _composer.ComposeContributionConfirmation(route.Subject.Name, email.Body, email.AttachmentNames)),
            false => (
                GiftIdeaEmailCompositionService.ConfirmationSubject,
                _composer.ComposeConfirmation(email.Body, email.AttachmentNames))
        };

        return _sender.SendAsync(route.Sender.Email, subject, body);
    }

    /// <summary>
    /// Finds what an address routes to, whichever of the two kinds of token it carries.
    /// </summary>
    /// <remarks>
    /// The participant's own address is tried first because it is much the commoner of the two, and
    /// the second lookup only happens for a message that would otherwise have been dropped. The
    /// token spaces do not overlap — every token is generated the same way and stored in one table
    /// or the other — so the order is a matter of cost rather than of precedence.
    /// </remarks>
    private async Task<(bool found, GiftIdeaRoute route)> ResolveRouteAsync(string tokenHash)
    {
        var own = await _giftExchangeProvider.FindGiftIdeaRouteAsync(tokenHash).ConfigureAwait(false);

        return own.found
            ? own
            : await _giftExchangeProvider.FindGiftIdeaContributionRouteAsync(tokenHash).ConfigureAwait(false);
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

    /// <summary>
    /// Tells the sender why their message could not be used, and how to send another.
    /// </summary>
    /// <remarks>
    /// The token goes with it. Only the address the message arrived at can say where the next
    /// attempt should go, and that address is not recoverable from the route — what is stored is a
    /// hash of the token, which is the point of storing a hash.
    /// </remarks>
    private async Task<GiftIdeaSubmissionOutcome> ReplyWithRejectionAsync(
        GiftIdeaRoute route,
        GiftIdeaSubmissionOutcome outcome,
        InboundEmail email,
        string giftIdeasToken
    )
    {
        _logger.LogInformation("Refused a gift ideas submission: {Outcome}", outcome);

        await _sender.SendAsync(
                route.Sender.Email,
                GiftIdeaEmailCompositionService.CouldNotShareSubject,
                _composer.ComposeRejection(new ComposeRejectionRequest
                {
                    Outcome = outcome,
                    DroppedAttachments = email.AttachmentNames,
                    GiftIdeasToken = giftIdeasToken,
                    IsContribution = route.IsContribution,
                    SubjectName = route.Subject.Name
                }))
            .ConfigureAwait(false);

        return outcome;
    }

    private Task ForwardToGiverAsync(GiftIdeaRoute route, string ideas)
    {
        // Nobody has drawn this participant, so there is nobody to forward to. The submission is
        // already stored, and whoever draws them later can be sent it then. A contribution always
        // has somebody — the asker — so this is only ever reached on the ordinary path.
        if (string.IsNullOrWhiteSpace(route.Giver.Email))
        {
            _logger.LogInformation("Stored a gift ideas submission with nobody yet to forward it to.");
            return Task.CompletedTask;
        }

        // Chosen together, and one send rather than two, for the reason given in
        // ConfirmToSenderAsync: the address is the same either way, and only the words differ.
        var (subject, body) = route.IsContribution switch
        {
            true => (
                GiftIdeaEmailCompositionService.ContributionForwardSubject(route.Sender.Name, route.Subject.Name),
                _composer.ComposeContributionForward(route.Sender.Name, route.Subject.Name, route.HatName, ideas)),
            false => (
                GiftIdeaEmailCompositionService.ForwardSubject(route.Sender.Name),
                _composer.ComposeForward(route.Sender.Name, route.HatName, ideas))
        };

        return _sender.SendAsync(route.Giver.Email, subject, body);
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
