using System.Text;
using System.Web;

namespace GiftExchange.Library.Services;

/// <summary>
/// The pages somebody sees after clicking the Ask button in their invitation.
/// </summary>
/// <remarks>
/// Self-contained HTML with inline styles and no scripts, images or external references. Partly
/// because there is no front end to serve these from and adding one for four short pages would be a
/// poor trade, and partly because a page reached from an email link is fetched by scanners and
/// proxies before any person sees it — the fewer things it asks the network for, the fewer ways
/// that goes wrong.
///
/// No script also means the form has to work as a form: checkboxes with the same name, posted to
/// the same address that rendered them. Nothing here needs anything more.
/// </remarks>
[UsedImplicitly]
public class AskPageComposer
{
    private const string AskUrl = "https://api.namesoutofahat.com/ask";

    /// <summary>
    /// The field every chosen participant is posted under. Repeated once per checkbox, which is
    /// what an unscripted form does with a multiple choice.
    /// </summary>
    public const string ChoiceField = "who";

    /// <summary>
    /// Below this many participants, asking a third party stops being anonymous in practice.
    /// </summary>
    /// <remarks>
    /// Whoever is asked knows the asker is neither themselves nor the person the ideas are about,
    /// so in an exchange of <c>n</c> they are choosing between <c>n - 2</c> people. At three that is
    /// one person and the anonymity is gone entirely; at four it is a coin toss; by six a guess is
    /// worth little. Six is where the warning stops, not where the risk does — which is why the
    /// warning describes the situation rather than pronouncing it safe or unsafe.
    ///
    /// The asker is told and then trusted with it, rather than the option being withheld. They know
    /// whether the person they have in mind is the sort to work it out, and this application does
    /// not.
    /// </remarks>
    private const int SmallExchangeThreshold = 6;

    /// <summary>
    /// The page the button in the invitation lands on: who to ask, not whether to ask.
    /// </summary>
    /// <remarks>
    /// This page exists because the button is a link in an email and following a link is a GET.
    /// Mail security scanners fetch those on delivery, so an endpoint that acted on the GET would
    /// send the Ask before the participant had read their invitation. A scanner fetching this
    /// renders a form and stops; only a person can submit it.
    ///
    /// The pick is offered first and ticked, because asking the person whose name you drew is the
    /// ordinary case, and everything below it is the escape hatch for when asking them directly
    /// would give the game away.
    /// </remarks>
    /// <param name="askToken"></param>
    /// <param name="notice">
    /// Shown above the form when a submission came back here. Empty on the way in, and this
    /// application's own words rather than anybody else's — it is placed as markup, so nothing a
    /// participant typed may be passed here.
    /// </param>
    /// <param name="subjectName"></param>
    /// <param name="candidates"></param>
    public string ComposeChoose(
        string subjectName,
        ImmutableList<AskCandidate> candidates,
        string askToken,
        string notice
    )
    {
        var encodedName = HttpUtility.HtmlEncode(subjectName);
        var action = $"{AskUrl}/{HttpUtility.UrlEncode(askToken)}";

        var pick = candidates.Where(candidate => candidate.IsTheirPick).ToImmutableList();
        var others = candidates.Where(candidate => !candidate.IsTheirPick).ToImmutableList();

        var body = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(notice))
            body.Append(
                $"""
                 <p style="margin:0 0 20px;padding:12px 16px;background-color:#fdf3d8;border-radius:4px;">{notice}</p>
                 """);

        body.Append(
            $"""
             <p>Choose who to ask. <b>Your name won't be revealed to any of them</b>, and anything
             they share comes straight to your inbox.</p>
             <form method="post" action="{HttpUtility.HtmlAttributeEncode(action)}">
             """);

        if (!pick.IsEmpty)
            body.Append(
                $"""
                 <p style="margin:24px 0 8px;font-weight:bold;">Ask {encodedName} directly</p>
                 {Choices(pick, "we'll ask what they'd like, without saying who wanted to know", true)}
                 """);

        if (!others.IsEmpty)
        {
            body.Append(
                $"""
                 <p style="margin:24px 0 8px;font-weight:bold;">Or ask anyone else what they think
                 {encodedName} would like</p>
                 """);

            // Before the checkboxes rather than after them. Somebody who has already ticked three
            // names has decided, and a caveat underneath is read as small print about a choice
            // already made.
            if (candidates.Count + 1 < SmallExchangeThreshold)
                body.Append(
                    $"""
                     <p style="margin:0 0 12px;padding:12px 16px;background-color:#fdf3d8;border-radius:4px;font-size:14px;">
                     <b>Worth knowing in a group this size:</b> we won't say who asked, but whoever
                     you ask knows it wasn't them and wasn't {encodedName} &mdash; so in an exchange
                     this small they may well work out that it was you.</p>
                     """);

            body.Append(Choices(others, $"we'll ask for ideas about {encodedName}", false));
        }

        body.Append(
            """
              <p style="margin:28px 0 0;">
                <button type="submit" style="background-color:#2f5d8a;color:#ffffff;padding:12px 22px;border:0;border-radius:4px;font-weight:bold;font-size:16px;cursor:pointer;">
                  Send
                </button>
              </p>
            </form>
            <p style="color:#666666;font-size:14px;">You can ask each person once a week, so nobody
            ends up being nagged. Anyone who replies will be named to you, so you'll know whose
            suggestion is whose.</p>
            """);

        return Page($"Gift ideas for {subjectName}", body.ToString());
    }

    /// <summary>
    /// What a round of asking actually did, name by name.
    /// </summary>
    /// <remarks>
    /// Reports each person separately rather than counting them. A round can partly succeed — some
    /// asked, some refused because this asker asked them recently — and a total gives the reader no
    /// way to tell which of the names they chose still needs another route.
    /// </remarks>
    public string ComposeAskResults(string subjectName, ImmutableList<AskAttempt> attempts)
    {
        var encodedName = HttpUtility.HtmlEncode(subjectName);
        var sent = attempts.Where(attempt => attempt.Sent).ToImmutableList();
        var skipped = attempts.Where(attempt => !attempt.Sent).ToImmutableList();

        var body = new StringBuilder();

        body.Append(sent.IsEmpty
            ? "<p>We didn't ask anyone this time.</p>"
            : $"""
               <p>We've asked {NameFormatting.ToSentenceList(sent.Select(attempt => attempt.Name))} for gift ideas for {encodedName}, without saying who
               wanted to know.</p>
               <p>If they share anything, it'll arrive in your inbox.</p>
               """);

        if (!skipped.IsEmpty)
        {
            body.Append("<p>We didn't ask these people, because you asked them recently:</p><ul>");

            foreach (var attempt in skipped)
                body.Append(attempt.PreviouslyAskedAt == DateTimeOffset.MinValue
                    ? $"<li>{HttpUtility.HtmlEncode(attempt.Name)} &mdash; asked recently</li>"
                    : $"<li>{HttpUtility.HtmlEncode(attempt.Name)} &mdash; asked on <b>{attempt.PreviouslyAskedAt:d MMMM yyyy}</b></li>");

            body.Append(
                """
                </ul>
                <p>You can ask each of them again after a week. We've emailed you this list too, in
                case you'd forgotten.</p>
                """);
        }

        body.Append("<p>You can close this page.</p>");

        return Page(sent.IsEmpty ? "Nothing sent" : "Asked!", body.ToString());
    }

    /// <summary>
    /// One page for every reason an Ask cannot happen.
    /// </summary>
    /// <remarks>
    /// Deliberately does not distinguish an unknown token from a finished exchange. Somebody
    /// holding a guessed token would otherwise learn from the difference whether it named a real
    /// participant.
    /// </remarks>
    public static string ComposeUnavailable() =>
        Page(
            "This link isn't available",
            """
            <p>We can't ask for gift ideas from this link. The gift exchange may have finished, or
            the link may have expired.</p>
            <p>If the exchange is still running, use the button in your most recent invitation
            email.</p>
            """);

    /// <summary>
    /// A labelled checkbox per candidate, all under the one field name.
    /// </summary>
    /// <remarks>
    /// The id is the value, and it is not a secret: a participant id says nothing on its own, the
    /// token in the address is what authorises the request, and the handler checks every id it is
    /// given against the asker's own exchange rather than trusting the form it rendered.
    /// </remarks>
    private static string Choices(ImmutableList<AskCandidate> candidates, string note, bool ticked)
    {
        var rows = new StringBuilder();

        foreach (var candidate in candidates)
        {
            var id = $"who-{candidate.ParticipantId}";

            rows.Append(
                $"""
                 <label for="{HttpUtility.HtmlAttributeEncode(id)}" style="display:block;padding:10px 12px;margin-bottom:6px;background-color:#faf8f5;border-radius:4px;cursor:pointer;">
                   <input type="checkbox" id="{HttpUtility.HtmlAttributeEncode(id)}" name="{ChoiceField}" value="{HttpUtility.HtmlAttributeEncode(candidate.ParticipantId.ToString())}"{(ticked ? " checked" : string.Empty)} style="margin-right:10px;" />
                   <b>{HttpUtility.HtmlEncode(candidate.Name)}</b>
                   <span style="color:#666666;font-size:14px;"> &mdash; {note}</span>
                 </label>
                 """);
        }

        return rows.ToString();
    }


    private static string Page(string heading, string body) =>
        $"""
         <!doctype html>
         <html lang="en">
         <head>
           <meta charset="utf-8" />
           <meta name="viewport" content="width=device-width, initial-scale=1" />
           <title>{HttpUtility.HtmlEncode(heading)} &mdash; Names Out Of A Hat</title>
         </head>
         <body style="margin:0;padding:24px;background-color:#faf8f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Helvetica,Arial,sans-serif;color:#222222;line-height:1.5;">
           <div style="max-width:560px;margin:0 auto;background-color:#ffffff;border-radius:8px;padding:32px;">
             <p style="margin:0 0 24px;font-size:20px;"><b>🎩 Names Out Of A Hat 🎩</b></p>
             <h1 style="margin:0 0 16px;font-size:24px;">{HttpUtility.HtmlEncode(heading)}</h1>
             {body}
           </div>
         </body>
         </html>
         """;
}
