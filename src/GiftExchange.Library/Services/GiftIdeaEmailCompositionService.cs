using System.Web;

namespace GiftExchange.Library.Services;

/// <summary>
/// The messages the inbound path sends: what went back to whoever wrote in, and what goes on to the
/// person their ideas are for.
/// </summary>
/// <remarks>
/// Everything a sender wrote is HTML-encoded on its way into these bodies and none of it is ever
/// turned into a link. That second part is deliberate and is most of what makes allowing links
/// tolerable: a URL arrives as text, so the person reading it sees where it actually goes rather
/// than words an anchor was wrapped around. Some mail clients will make it clickable at the far
/// end, which is fine — what matters is that the address is visible either way, and that this
/// application is not the thing that hid it.
/// </remarks>
[UsedImplicitly]
public class GiftIdeaEmailCompositionService
{
    public static string ConfirmationSubject => "We shared your gift ideas";

    public static string CouldNotShareSubject => "We couldn't share your gift ideas";

    public static string DoNotReplySubject => "This address isn't monitored";

    public static string ForwardSubject(string senderName) =>
        $"{senderName} shared gift ideas with you";

    /// <summary>
    /// Sent back to the participant once their ideas are on their way.
    /// </summary>
    /// <remarks>
    /// The echo is the reason this exists. What gets stored is text pulled out of an email, which
    /// means a guess was made about where their message ended and a quoted one began. Showing them
    /// exactly what was kept turns a bad guess into something they can see and correct, rather than
    /// something that quietly goes out wrong.
    /// </remarks>
    public string ComposeConfirmation(string ideas, ImmutableList<string> droppedAttachments) =>
        Wrap([
            "Thanks — your gift ideas are on their way to the person who picked your name.",
            "Here's exactly what we shared:",
            Quote(ideas),
            ..DroppedAttachmentsNote(droppedAttachments),
            "Changed your mind? Send another email to the same address and it replaces this one.",
            "Nobody else in the exchange sees this — not even the organizer."
        ]);

    /// <summary>
    /// Sent back when the submission cannot be used, saying which of the rules it ran into.
    /// </summary>
    /// <remarks>
    /// Every branch says what to do next, because a reply that only says no leaves somebody with a
    /// gift exchange they cannot take part in and no idea why.
    /// </remarks>
    public string ComposeRejection(GiftIdeaSubmissionOutcome outcome, ImmutableList<string> droppedAttachments) =>
        Wrap([
            "We couldn't share what you sent.",
            ExplainRejection(outcome),
            ..DroppedAttachmentsNote(droppedAttachments),
            "Reply to this email with a new message and we'll try again."
        ]);

    /// <summary>
    /// Carries one participant's ideas to the person who drew them.
    /// </summary>
    /// <remarks>
    /// Comes from the no-reply address and carries no way back to the sender, which is not a
    /// courtesy but the whole arrangement: a reply that reached them would tell them who holds
    /// their name. The recipient already knows whose name they drew, so naming the sender here
    /// gives away nothing.
    /// </remarks>
    public string ComposeForward(string senderName, string hatName, string ideas) =>
        Wrap([
            $"<b>{HttpUtility.HtmlEncode(senderName)}</b>, whose name you picked in {HttpUtility.HtmlEncode(GetQualifiedName(hatName))}, shared some gift ideas with you:",
            Quote(ideas),
            "<i>You're the only person seeing this. Please don't reply to this email — your reply would reveal that you have their name.</i>",
            BuildLinkDisclaimer()
        ]);

    /// <summary>
    /// Sent to somebody who wrote to the no-reply address, which is a reasonable thing to try and
    /// currently gets no response at all.
    /// </summary>
    public string ComposeDoNotReply() =>
        Wrap([
            "This email address isn't monitored by a human, so nobody has read what you sent.",
            "If you want to share gift ideas, use the <b>SHARE GIFT IDEAS</b> button from your invitation. It opens a new email addressed to the right place.",
            "If you need to reach the person running your gift exchange, reply to their invitation directly or contact them yourself — we can't pass a message on for you."
        ]);

    private static string ExplainRejection(GiftIdeaSubmissionOutcome outcome) =>
        outcome switch
        {
            GiftIdeaSubmissionOutcome.RejectedNothingToShare =>
                "We couldn't find any text in your message. Write your ideas in the body of the email and send it again.",

            GiftIdeaSubmissionOutcome.RejectedTooLong =>
                $"Your message is longer than we can handle — please shorten it to under about {GiftIdeaContentPolicy.MaxBodyBytes / 1000},000 characters and send it again.",

            // Said carefully. The likeliest cause by far is that they forwarded their invitation
            // rather than using the button, which is an easy mistake and not a suspicious one.
            GiftIdeaSubmissionOutcome.RejectedWouldRevealTheirPick =>
                "Your message mentions the name of the person you picked. That usually happens when an invitation gets forwarded or quoted, and we can't pass it on — it would tell the person reading it whose name you drew. Please send just your gift ideas, in a fresh email.",

            GiftIdeaSubmissionOutcome.RejectedShortenedLink =>
                "Your message contains a shortened link. We can't show the person receiving it where a shortened link leads, so please paste the full web address instead.",

            GiftIdeaSubmissionOutcome.RejectedSelfReferentialLink =>
                "Your message links back to namesoutofahat.com. We don't pass those on. Please send your gift ideas without it.",

            GiftIdeaSubmissionOutcome.RejectedTooManyLinks =>
                $"Your message contains more than {GiftIdeaContentPolicy.MaxLinks} links. Please send a shorter list.",

            GiftIdeaSubmissionOutcome.RejectedInappropriateContent =>
                "Your message contains content we can't pass on. Please reword it and send it again.",

            // Distinct from the line above on purpose: their message may be perfectly fine, and
            // telling somebody their gift ideas were inappropriate when the checker was simply
            // unreachable is both wrong and unhelpful.
            GiftIdeaSubmissionOutcome.RejectedModerationUnavailable =>
                "We couldn't check your message just now. Please send it again in a few minutes.",

            GiftIdeaSubmissionOutcome.RejectedExchangeNotAcceptingIdeas =>
                "This gift exchange has finished, so there's nobody left to share ideas with.",

            _ => "Something went wrong at our end. Please try again."
        };

    /// <summary>
    /// Named, not silently discarded. Somebody who attached a photo of the thing they want should
    /// find out that it did not go, rather than assume it did.
    /// </summary>
    private static IEnumerable<string> DroppedAttachmentsNote(ImmutableList<string> droppedAttachments)
    {
        if (droppedAttachments.Count == 0)
            yield break;

        var names = string.Join(", ", droppedAttachments.Select(HttpUtility.HtmlEncode));

        yield return $"""
                      <i>You attached {names}, which we couldn't include — we're only able to pass on the text of your message. If the attachment matters, describe it or include a link instead.</i>
                      """;
    }

    /// <summary>
    /// Says plainly that links are the sender's own and are not checked.
    /// </summary>
    /// <remarks>
    /// The same honesty the invitation small print already practises about organizer-supplied text.
    /// Nothing here can tell whether a link is safe, and implying otherwise by staying quiet would
    /// be worse than saying so.
    /// </remarks>
    private static string BuildLinkDisclaimer() =>
        """
        <small style="color:#666666;">
        These are their own words, including any links. We don't check where links go, so only follow one if you trust it. Links are shown in full so you can see where they lead before clicking.
        </small>
        """;

    /// <summary>
    /// Renders submitted text as an indented block, encoded and with line breaks preserved.
    /// </summary>
    /// <remarks>
    /// <c>HtmlEncode</c> first and then newlines to breaks, in that order. Reversed, the tags this
    /// inserts would themselves be encoded and the recipient would read the markup.
    /// </remarks>
    private static string Quote(string ideas) =>
        $"""
         <div style="border-left:3px solid #cccccc;padding-left:12px;color:#333333;">
         {HttpUtility.HtmlEncode(ideas).Replace("\n", "<br />")}
         </div>
         """;

    private static string Wrap(IEnumerable<string> lines) =>
        string.Join(
            "<br /><br />",
            lines.Where(line => !string.IsNullOrWhiteSpace(line)))
        + """<br /><br /><a href="https://namesoutofahat.com"><b>🎩 Names Out Of A Hat 🎩</b></a>""";

    private static string GetQualifiedName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? "your gift exchange"
            : name.StartsWith("the ", StringComparison.OrdinalIgnoreCase) ? name : $"the {name}";
}
