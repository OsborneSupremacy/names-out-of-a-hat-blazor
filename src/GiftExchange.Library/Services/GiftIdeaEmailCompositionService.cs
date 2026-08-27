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

    public static string AskSubject(string hatName) =>
        $"What would you like for {GiftExchangeNaming.Describe(hatName)}?";

    public static string AskThrottledSubject => "You've already asked recently";

    /// <summary>
    /// Where gift ideas are received. A subdomain of its own, not the one invitations are sent
    /// from: an MX record there would also catch the DMARC reports that already arrive at
    /// mail.namesoutofahat.com, and SES receipt rules match a whole domain or one exact address,
    /// with no way to claim a prefix.
    /// </summary>
    private const string GiftIdeasDomain = "ideas.namesoutofahat.com";

    /// <summary>Where the Ask button points. The API, not the front end.</summary>
    private const string AskUrl = "https://api.namesoutofahat.com/ask";

    /// <summary>
    /// The block inviting somebody to share gift ideas, addressed to the token issued to them.
    /// </summary>
    /// <remarks>
    /// A <c>mailto:</c> link rather than a reply, and that distinction is doing security work
    /// rather than cosmetic work. An invitation names the recipient's own pick, so a reply would
    /// quote it, the quoted text would be hard to strip reliably across mail clients, and what
    /// leaked would be their own pick, forwarded to the one person who must never learn it.
    /// Clicking here opens an empty message instead, so there is nothing to quote and nothing to
    /// strip.
    ///
    /// The address appears in full underneath, because a mail client that has not been registered
    /// as the handler for mailto: links does nothing at all when this is clicked, with no error to
    /// explain the silence.
    ///
    /// Shared between the invitation and the Ask, so that both carry the identical block and the
    /// wording cannot drift between them.
    /// </remarks>
    public static string BuildShareGiftIdeasBlock(string giftIdeasToken)
    {
        var address = $"{giftIdeasToken}@{GiftIdeasDomain}";
        var mailto = $"mailto:{HttpUtility.UrlEncode(giftIdeasToken)}@{GiftIdeasDomain}?subject=My%20gift%20ideas";

        return $"""
                <a href="{HttpUtility.HtmlAttributeEncode(mailto)}" style="background-color:#1f7a4d;color:#ffffff;padding:12px 22px;text-decoration:none;border-radius:4px;display:inline-block;font-weight:bold;">SHARE GIFT IDEAS</a>
                <br /><br />
                Click above to share gift ideas with only the person who picked your name. Nobody else in the exchange will see them &mdash; not even the organizer. Your email will open with the address already filled in; just type your ideas and send. We'll email you back to confirm exactly what was shared.
                <br /><br />
                <small style="color:#666666;">Button not working? Send your ideas to {HttpUtility.HtmlEncode(address)}</small>
                """;
    }

    /// <summary>
    /// The block offering to ask the recipient's own pick for gift ideas on their behalf.
    /// </summary>
    /// <remarks>
    /// An ordinary link rather than a mailto:, because what it triggers happens on our side. It
    /// lands on a confirmation page instead of performing the Ask outright, and that is deliberate:
    /// a link in an email is fetched by mail security scanners before anybody reads it, so a link
    /// that acted immediately would send the Ask on delivery — burning the throttle window and
    /// mailing somebody on behalf of a person who never clicked anything.
    /// </remarks>
    public static string BuildAskBlock(string pickedName, string askToken)
    {
        var encodedName = HttpUtility.HtmlEncode(pickedName);
        var url = $"{AskUrl}/{HttpUtility.UrlEncode(askToken)}";

        return $"""
                <a href="{HttpUtility.HtmlAttributeEncode(url)}" style="background-color:#2f5d8a;color:#ffffff;padding:12px 22px;text-decoration:none;border-radius:4px;display:inline-block;font-weight:bold;">ANONYMOUSLY ASK {encodedName.ToUpperInvariant()} FOR GIFT IDEAS</a>
                <br /><br />
                Click above to anonymously ask {encodedName} for gift ideas. Your name will not be revealed. If {encodedName} responds with gift ideas, we'll send them to you.
                """;
    }

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
            $"<b>{HttpUtility.HtmlEncode(senderName)}</b>, whose name you picked in {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hatName))}, shared some gift ideas with you:",
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

    /// <summary>
    /// Sent to the person somebody has asked for gift ideas.
    /// </summary>
    /// <remarks>
    /// Names nobody. The whole promise of the button that triggers this is anonymity, and in an
    /// exchange this small, naming the asker would be naming the one person holding this
    /// recipient's name. "Someone" is doing real work in that first line.
    ///
    /// It also gives nothing away by existing: everybody is drawn by exactly one person, so
    /// learning that somebody has your name tells you what you already knew.
    /// </remarks>
    public string ComposeAsk(string hatName, string giftIdeasToken) =>
        Wrap([
            $"Someone in {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hatName))} would like to know what you'd like.",
            "They picked your name, and they're hoping for a hint. You can share as much or as little as you like.",
            BuildShareGiftIdeasBlock(giftIdeasToken),
            "<i>We won't tell you who asked, and we won't tell them we passed the message on.</i>"
        ]);

    /// <summary>
    /// Sent to somebody who asked again too soon.
    /// </summary>
    /// <remarks>
    /// Says when the last Ask went out, because the likeliest reader is somebody who does not
    /// remember making it — and being told a date is what turns "you already asked" from an
    /// accusation into a fact they can check.
    /// </remarks>
    public string ComposeAskThrottled(string pickedName, DateTimeOffset previouslyAskedAt) =>
        Wrap([
            previouslyAskedAt == DateTimeOffset.MinValue
                ? $"We've already asked {HttpUtility.HtmlEncode(pickedName)} to share gift ideas on your behalf recently."
                : $"We asked {HttpUtility.HtmlEncode(pickedName)} to share gift ideas on your behalf on {previouslyAskedAt:d MMMM yyyy}.",
            "You'll need to wait a while before asking again.",
            $"If {HttpUtility.HtmlEncode(pickedName)} shares anything, we'll send it to you as soon as they do."
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
}
