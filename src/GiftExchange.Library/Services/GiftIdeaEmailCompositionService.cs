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

    /// <summary>
    /// Sent to somebody asked for ideas about another participant.
    /// </summary>
    /// <remarks>
    /// Names the subject, unlike <see cref="AskSubject"/>, which cannot: that one goes to the
    /// person it is about, and naming them in the subject line would read as though we were telling
    /// them something they did not know. Here the name is the whole point — a subject line reading
    /// "can you help with a gift?" tells the reader nothing about whether they can.
    /// </remarks>
    public static string ContributionAskSubject(string subjectName) =>
        $"Any gift ideas for {subjectName}?";

    public static string ContributionConfirmationSubject(string subjectName) =>
        $"We shared your gift ideas for {subjectName}";

    public static string ContributionForwardSubject(string helperName, string subjectName) =>
        $"{helperName} shared gift ideas for {subjectName}";

    /// <summary>
    /// Sent when an Ask went out to some of the people chosen but not all of them.
    /// </summary>
    /// <remarks>
    /// Says outright that something did not happen. The email exists only in that case, so a
    /// subject line hedging about the outcome would waste the one line the reader is certain to
    /// see.
    /// </remarks>
    public static string AskPartiallySentSubject => "We couldn't ask everyone you chose";

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
    /// The block inviting somebody to share gift ideas about themselves, addressed to the token
    /// issued to them.
    /// </summary>
    /// <remarks>
    /// Shared between the invitation and the Ask, so that both carry the identical block and the
    /// wording cannot drift between them.
    /// </remarks>
    public static string BuildShareGiftIdeasBlock(string giftIdeasToken) =>
        BuildShareBlock(
            giftIdeasToken,
            "SHARE GIFT IDEAS",
            "My gift ideas",
            "Click above to share gift ideas with only the person who picked your name. Nobody else in the exchange will see them &mdash; not even the organizer.");

    /// <summary>
    /// The same block, for somebody suggesting gifts for another participant rather than for
    /// themselves.
    /// </summary>
    /// <remarks>
    /// Says that the ideas will be attributed. That is the one thing about this arrangement the
    /// helper cannot work out for themselves — the asker is anonymous to them, so the natural
    /// assumption is that the anonymity runs both ways — and somebody who would rather stay out of
    /// it is entitled to know before they write anything, not afterwards.
    /// </remarks>
    public static string BuildShareIdeasAboutBlock(string subjectName, string askToken)
    {
        var encodedName = HttpUtility.HtmlEncode(subjectName);

        return BuildShareBlock(
            askToken,
            $"SHARE GIFT IDEAS FOR {HttpUtility.HtmlEncode(subjectName.ToUpperInvariant())}",
            $"Gift ideas for {subjectName}",
            $"Click above to send your ideas to the person shopping for {encodedName}. They'll see the ideas came from you. Nobody else will &mdash; not {encodedName}, and not the organizer.");
    }

    /// <summary>
    /// A mailto: button, the sentence explaining it, and the address in full underneath.
    /// </summary>
    /// <remarks>
    /// A <c>mailto:</c> link rather than a reply, and that distinction is doing security work
    /// rather than cosmetic work. The email carrying this button may name somebody's own pick, so a
    /// reply would quote it, the quoted text would be hard to strip reliably across mail clients,
    /// and what leaked would be their pick, forwarded to the one person who must never learn it.
    /// Clicking here opens an empty message instead, so there is nothing to quote and nothing to
    /// strip.
    ///
    /// The address appears in full underneath, because a mail client that has not been registered
    /// as the handler for mailto: links does nothing at all when this is clicked, with no error to
    /// explain the silence.
    ///
    /// One implementation behind both callers, so that the parts neither of them should be free to
    /// reword &mdash; how to send, and the promise of an echo &mdash; cannot drift apart.
    /// </remarks>
    private static string BuildShareBlock(
        string token,
        string buttonLabel,
        string mailSubject,
        string explanation
    )
    {
        var address = $"{token}@{GiftIdeasDomain}";
        var mailto =
            $"mailto:{HttpUtility.UrlEncode(token)}@{GiftIdeasDomain}?subject={HttpUtility.UrlEncode(mailSubject)}";

        return $"""
                <a href="{HttpUtility.HtmlAttributeEncode(mailto)}" style="background-color:#1f7a4d;color:#ffffff;padding:12px 22px;text-decoration:none;border-radius:4px;display:inline-block;font-weight:bold;">{buttonLabel}</a>
                <br /><br />
                {explanation} Your email will open with the address already filled in; just type your ideas and send. We'll email you back to confirm exactly what was shared.
                <br /><br />
                <small style="color:#666666;">Button not working? Send your ideas to {HttpUtility.HtmlEncode(address)}</small>
                """;
    }

    /// <summary>
    /// The block offering to ask for gift ideas about the recipient's own pick, on their behalf.
    /// </summary>
    /// <remarks>
    /// An ordinary link rather than a mailto:, because what it triggers happens on our side. It
    /// lands on a page instead of performing the Ask outright, and that is deliberate: a link in an
    /// email is fetched by mail security scanners before anybody reads it, so a link that acted
    /// immediately would send the Ask on delivery — burning the throttle window and mailing
    /// somebody on behalf of a person who never clicked anything.
    ///
    /// The button no longer names a single person to be asked, because the page behind it offers
    /// the whole exchange. It names the person the ideas are wanted <em>about</em> instead, which
    /// is the part the reader already has in mind — they have just been told that name.
    /// </remarks>
    public static string BuildAskBlock(string pickedName, string askToken)
    {
        var encodedName = HttpUtility.HtmlEncode(pickedName);
        var url = $"{AskUrl}/{HttpUtility.UrlEncode(askToken)}";

        return $"""
                <a href="{HttpUtility.HtmlAttributeEncode(url)}" style="background-color:#2f5d8a;color:#ffffff;padding:12px 22px;text-decoration:none;border-radius:4px;display:inline-block;font-weight:bold;">ANONYMOUSLY ASK ABOUT GIFTS FOR {HttpUtility.HtmlEncode(pickedName.ToUpperInvariant())}</a>
                <br /><br />
                Click above to ask {encodedName} what they'd like &mdash; or, if asking {encodedName} directly would give the game away, to ask anyone else in the exchange what they think {encodedName} would like. You can ask several people at once. Your name will not be revealed to any of them, and anything they share comes straight to you.
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
    public string ComposeRejection(ComposeRejectionRequest request) =>
        Wrap([
            "We couldn't share what you sent.",
            ExplainRejection(request.Outcome),
            ..DroppedAttachmentsNote(request.DroppedAttachments),
            ..TryAgainNote(request)
        ]);

    /// <summary>
    /// The way back after a refusal: the same button the invitation carried, addressed to the same
    /// place the refused message was sent.
    /// </summary>
    /// <remarks>
    /// This used to say "reply to this email", which was wrong twice over. Everything here goes out
    /// from the no-reply address, so a reply landed on the mailbox that answers only that nobody
    /// read it — and that answer is throttled to one a day, so somebody who tried twice got silence
    /// the second time. It was also the worst available advice for the commonest refusal: a message
    /// refused for naming the sender's own pick had quoted their invitation, and a reply would have
    /// quoted something again. A mailto: opens an empty message, so there is nothing to quote.
    ///
    /// Says outright not to reply, rather than leaving the button to imply it. The reply is the
    /// obvious move and the button is one more thing to notice, so the sentence has to beat the
    /// habit.
    /// </remarks>
    private static IEnumerable<string> TryAgainNote(ComposeRejectionRequest request)
    {
        // Nothing to offer somebody whose exchange has finished. The next message would meet the
        // same refusal, and a button inviting one would be promising something that cannot happen.
        if (request.Outcome == GiftIdeaSubmissionOutcome.RejectedExchangeNotAcceptingIdeas)
            yield break;

        yield return "Please send a new email rather than replying to this one &mdash; replies reach an address nobody reads. The button below opens an empty message addressed to the right place.";

        yield return request.IsContribution
            ? BuildShareIdeasAboutBlock(request.SubjectName, request.GiftIdeasToken)
            : BuildShareGiftIdeasBlock(request.GiftIdeasToken);
    }

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
            $"Someone in {HttpUtility.HtmlEncode(GiftExchangeNaming.DescribeMidSentence(hatName))} would like to know what you'd like.",
            "They picked your name, and they're hoping for a hint. You can share as much or as little as you like.",
            BuildShareGiftIdeasBlock(giftIdeasToken),
            "<i>We won't tell you who asked, and we won't tell them we passed the message on.</i>"
        ]);

    /// <summary>
    /// Sent to somebody a participant has asked for ideas about another participant.
    /// </summary>
    /// <remarks>
    /// Names the subject and nobody else. Whoever asked stays out of it, which is the promise the
    /// button makes — and in a small exchange it is a promise this email cannot fully keep, since a
    /// reader who works out that only one person could have drawn the subject has their answer. The
    /// page that sends this says so plainly to the one person who can weigh it, which is the asker.
    /// Repeating it here would only hand the helper the deduction.
    ///
    /// Says that anything shared will be attributed, before the button rather than after it.
    /// </remarks>
    public string ComposeContributionAsk(string hatName, string subjectName, string askToken)
    {
        var encodedName = HttpUtility.HtmlEncode(subjectName);

        return Wrap([
            $"Someone in {HttpUtility.HtmlEncode(GiftExchangeNaming.DescribeMidSentence(hatName))} drew {encodedName}'s name, and they're hoping you might know what {encodedName} would like.",
            $"Anything helps &mdash; something {encodedName} has been after, a hobby, a size, a shop they like, or a link to something you've seen them admire. You can share as much or as little as you want, and you're welcome to ignore this.",
            BuildShareIdeasAboutBlock(subjectName, askToken),
            $"<i>We won't tell you who asked. {encodedName} isn't being told about this either, and nothing you send goes to them &mdash; only to the person shopping for them.</i>"
        ]);
    }

    /// <summary>
    /// Sent to an asker when some of the people they chose were not asked.
    /// </summary>
    /// <remarks>
    /// Only when something was refused. Every outcome is already on the page they are looking at
    /// when they submit, and an email repeating a round that went through exactly as asked would be
    /// one more message in an inbox for no reason. A round that fell short is different: the page
    /// is gone the moment they close the tab, and what they need to remember is the part that did
    /// not happen.
    ///
    /// Names each person and the date, because the likeliest reader is somebody who does not
    /// remember asking — and being told a date is what turns "you already asked" from an accusation
    /// into a fact they can check.
    /// </remarks>
    public string ComposeAskSummary(string subjectName, ImmutableList<AskAttempt> attempts)
    {
        var encodedName = HttpUtility.HtmlEncode(subjectName);
        var sent = attempts.Where(attempt => attempt.Sent).ToImmutableList();
        var skipped = attempts.Where(attempt => !attempt.Sent).ToImmutableList();

        var lines = new List<string>
        {
            sent.IsEmpty
                ? $"We weren't able to ask anyone for gift ideas for {encodedName} just now."
                : $"We asked {NameFormatting.ToSentenceList(sent.Select(attempt => attempt.Name))} for gift ideas for {encodedName}, without saying who wanted to know."
        };

        // Defensive: nothing sends this email unless somebody was skipped, and a list with no names
        // under a sentence introducing them would read as a fault in the application.
        if (!skipped.IsEmpty)
        {
            lines.Add("We didn't ask these people, because you asked them recently:");
            lines.Add(Block(string.Join(
                "<br />",
                skipped.Select(attempt => attempt.PreviouslyAskedAt == DateTimeOffset.MinValue
                    ? $"{HttpUtility.HtmlEncode(attempt.Name)} &mdash; asked recently"
                    : $"{HttpUtility.HtmlEncode(attempt.Name)} &mdash; asked on {attempt.PreviouslyAskedAt:d MMMM yyyy}"))));
            lines.Add("You can ask each of them again after a week. Nobody is told how many people you've asked, or how often.");
        }

        lines.Add("If any of them shares anything, we'll send it to you as soon as they do.");

        return Wrap(lines);
    }

    /// <summary>
    /// Carries one participant's suggestions about another to the person who asked for them.
    /// </summary>
    /// <remarks>
    /// Names the helper, which the ordinary forward also does and for a stronger reason here: the
    /// asker chose who to ask, so this tells them nothing they did not already know, and when
    /// several people answer, an unattributed pile of suggestions is much less use than an
    /// attributed one.
    ///
    /// Comes from the no-reply address and carries no way back, like everything else here. There is
    /// nothing secret about the asker's identity to protect from the helper at this point — the
    /// helper never sees this message — but a reply that reached them would tell them who asked.
    /// </remarks>
    public string ComposeContributionForward(string helperName, string subjectName, string hatName, string ideas) =>
        Wrap([
            $"<b>{HttpUtility.HtmlEncode(helperName)}</b> has some ideas about what {HttpUtility.HtmlEncode(subjectName)} might like, after you asked in {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hatName))}:",
            Quote(ideas),
            $"<i>You're the only person we've shown this to, and we didn't tell {HttpUtility.HtmlEncode(helperName)} who was asking. Please don't reply to this email &mdash; nobody reads this address.</i>",
            BuildLinkDisclaimer()
        ]);

    /// <summary>
    /// Sent back to somebody once their suggestions about another participant are on their way.
    /// </summary>
    /// <remarks>
    /// Echoes what was kept, for the reason <see cref="ComposeConfirmation"/> does, and repeats
    /// that the ideas are attributed. Somebody who only reads one of the two emails should still
    /// find that out.
    /// </remarks>
    public string ComposeContributionConfirmation(
        string subjectName,
        string ideas,
        ImmutableList<string> droppedAttachments
    )
    {
        var encodedName = HttpUtility.HtmlEncode(subjectName);

        return Wrap([
            $"Thanks &mdash; your ideas are on their way to the person shopping for {encodedName}.",
            "Here's exactly what we shared:",
            Quote(ideas),
            ..DroppedAttachmentsNote(droppedAttachments),
            "They'll see that these came from you.",
            "Changed your mind? Send another email to the same address and it replaces this one.",
            $"Nobody else sees this &mdash; not {encodedName}, and not the organizer."
        ]);
    }


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
        Block(HttpUtility.HtmlEncode(ideas).Replace("\n", "<br />"));

    /// <summary>
    /// The indented frame, around markup this class built rather than around somebody's text.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="Quote"/> and never given anything a sender wrote. Everything that
    /// arrives from outside goes through Quote, which encodes it first; the split exists so that
    /// the encoding step cannot be skipped by a caller reaching for the frame it wanted.
    /// </remarks>
    private static string Block(string html) =>
        $"""
         <div style="border-left:3px solid #cccccc;padding-left:12px;color:#333333;">
         {html}
         </div>
         """;

    private static string Wrap(IEnumerable<string> lines) =>
        EmailBranding.Masthead()
        + "<br /><br />"
        + string.Join(
            "<br /><br />",
            lines.Where(line => !string.IsNullOrWhiteSpace(line)))
        + "<br /><br />"
        + EmailBranding.SignOff();
}
