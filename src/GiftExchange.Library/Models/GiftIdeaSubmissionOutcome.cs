namespace GiftExchange.Library.Models;

/// <summary>
/// What became of one inbound gift ideas email.
///
/// The distinction that matters is whether the sender hears back. Anything named Dropped is
/// discarded in silence, because at that point nothing has established who actually sent it, and
/// replying to an unauthenticated address is how a mail system becomes a backscatter source
/// pointed at strangers. Anything named Rejected has passed that bar — a valid token, and a From
/// matching the participant it belongs to — so there is a real person to tell.
/// </summary>
public enum GiftIdeaSubmissionOutcome
{
    /// <summary>Stored, forwarded to whoever drew the sender, and confirmed back to them.</summary>
    Shared,

    /// <summary>SES could not vouch for the message: SPF, DKIM, DMARC, spam or virus.</summary>
    DroppedFailedAuthentication,

    /// <summary>
    /// An out of office, a bounce, or anything else with no person behind it. Replying would start
    /// a loop between two robots, and a bounce has no return path to reply down in any case.
    /// </summary>
    DroppedAutomatedMessage,

    /// <summary>No live token matches the address it was sent to.</summary>
    DroppedUnknownToken,

    /// <summary>A valid token, but sent from an address that is not the participant's.</summary>
    DroppedSenderMismatch,

    /// <summary>The exchange is over, so there is nothing useful to do with this.</summary>
    RejectedExchangeNotAcceptingIdeas,

    /// <summary>Nothing left once quoting and signatures were removed.</summary>
    RejectedNothingToShare,

    /// <summary>Longer than the application will put through moderation.</summary>
    RejectedTooLong,

    /// <summary>
    /// The text contains the name of the person the sender drew, which means their own invitation
    /// has been quoted into it. Forwarding that would tell the recipient who the sender drew.
    /// </summary>
    RejectedWouldRevealTheirPick,

    /// <summary>A shortened link, whose destination cannot be shown to the person receiving it.</summary>
    RejectedShortenedLink,

    /// <summary>A link back to this application, which no gift idea needs and phishing does.</summary>
    RejectedSelfReferentialLink,

    /// <summary>More links than a list of gift ideas has any reason to carry.</summary>
    RejectedTooManyLinks,

    /// <summary>Content moderation refused it.</summary>
    RejectedInappropriateContent,

    /// <summary>Content moderation could not be reached, and unchecked text is not forwarded.</summary>
    RejectedModerationUnavailable,

    /// <summary>Sent to the do-not-reply address rather than a gift ideas one.</summary>
    RedirectedFromDoNotReply
}

public static class GiftIdeaSubmissionOutcomes
{
    /// <summary>
    /// Whether the sender is told what happened. False for everything discarded before the sender
    /// was established, which is what keeps this from being usable as a way to mail strangers.
    /// </summary>
    public static bool WarrantsAReply(this GiftIdeaSubmissionOutcome outcome) =>
        outcome is not (GiftIdeaSubmissionOutcome.DroppedFailedAuthentication
            or GiftIdeaSubmissionOutcome.DroppedAutomatedMessage
            or GiftIdeaSubmissionOutcome.DroppedUnknownToken
            or GiftIdeaSubmissionOutcome.DroppedSenderMismatch);
}
