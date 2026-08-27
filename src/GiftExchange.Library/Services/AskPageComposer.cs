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
/// </remarks>
[UsedImplicitly]
public class AskPageComposer
{
    private const string AskUrl = "https://api.namesoutofahat.com/ask";

    /// <summary>
    /// The page the button in the invitation actually lands on: a question, not an outcome.
    /// </summary>
    /// <remarks>
    /// This page exists because the button is a link in an email and following a link is a GET.
    /// Mail security scanners fetch those on delivery, so an endpoint that acted on the GET would
    /// send the Ask before the participant had read their invitation. A scanner fetching this
    /// renders a form and stops; only a person can submit it.
    /// </remarks>
    public string ComposeConfirm(string pickedName, string askToken)
    {
        var encodedName = HttpUtility.HtmlEncode(pickedName);
        var action = $"{AskUrl}/{HttpUtility.UrlEncode(askToken)}";

        return Page(
            $"Ask {encodedName} for gift ideas?",
            $"""
             <p>We'll email {encodedName} to say that someone in your gift exchange would like a hint
             about what to get them.</p>
             <p><b>Your name won't be revealed.</b> They won't be told who asked, and we won't tell
             them we passed the message on. If they share anything, we'll send it straight to you.</p>
             <form method="post" action="{HttpUtility.HtmlAttributeEncode(action)}">
               <button type="submit" style="background-color:#2f5d8a;color:#ffffff;padding:12px 22px;border:0;border-radius:4px;font-weight:bold;font-size:16px;cursor:pointer;">
                 Yes, ask {encodedName}
               </button>
             </form>
             <p style="color:#666666;font-size:14px;">You can only ask once a week, so nobody ends up
             being nagged.</p>
             """);
    }

    public string ComposeSent(string pickedName) =>
        Page(
            "Asked!",
            $"""
             <p>We've asked {HttpUtility.HtmlEncode(pickedName)} for gift ideas, without saying who
             wanted to know.</p>
             <p>If they share anything, it'll arrive in your inbox. You can close this page.</p>
             """);

    /// <summary>
    /// Shown when the throttle refuses. Says the date, for the same reason the email does: the
    /// likeliest reader is somebody who does not remember asking.
    /// </summary>
    public string ComposeAlreadyAsked(string pickedName, DateTimeOffset previouslyAskedAt)
    {
        var encodedName = HttpUtility.HtmlEncode(pickedName);

        var when = previouslyAskedAt == DateTimeOffset.MinValue
            ? $"We've already asked {encodedName} on your behalf recently."
            : $"We asked {encodedName} on your behalf on <b>{previouslyAskedAt:d MMMM yyyy}</b>.";

        return Page(
            "You've already asked",
            $"""
             <p>{when}</p>
             <p>You'll need to wait a while before asking again — we've sent you an email saying the
             same thing, in case you'd forgotten.</p>
             <p>If {encodedName} shares anything, we'll send it to you as soon as they do.</p>
             """);
    }

    /// <summary>
    /// One page for every reason an Ask cannot happen.
    /// </summary>
    /// <remarks>
    /// Deliberately does not distinguish an unknown token from a finished exchange. Somebody
    /// holding a guessed token would otherwise learn from the difference whether it named a real
    /// participant.
    /// </remarks>
    public string ComposeUnavailable() =>
        Page(
            "This link isn't available",
            """
            <p>We can't ask for gift ideas from this link. The gift exchange may have finished, or
            the link may have expired.</p>
            <p>If the exchange is still running, use the button in your most recent invitation
            email.</p>
            """);

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
