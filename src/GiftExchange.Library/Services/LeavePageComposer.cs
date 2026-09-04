using System.Web;

namespace GiftExchange.Library.Services;

/// <summary>
/// The pages somebody sees after following the leave link in the fine print of their invitation.
/// </summary>
/// <remarks>
/// Two endpoints for one action, as the Ask is, and for the same reason: the link lives in an email,
/// following it is a GET, and mail security scanners fetch links in delivered mail to check them. A
/// GET that removed the participant would therefore fire on delivery for a share of recipients —
/// somebody pulled out of an exchange they had not yet read they were in, an organizer told to draw
/// names again, and everybody else told to disregard theirs. So the GET renders a form, which a
/// scanner is welcome to fetch as often as it likes, and the POST behind the button does the work.
///
/// The shell around each page is <see cref="EmailLinkedPage"/>, shared with the Ask.
/// </remarks>
[UsedImplicitly]
public class LeavePageComposer
{
    /// <summary>The field the "never let this organizer add me again" checkbox is posted under.</summary>
    public const string BlockOrganizerField = "block-organizer";

    /// <summary>The field the "never let anybody add me again" checkbox is posted under.</summary>
    public const string BlockAnywhereField = "block-anywhere";

    /// <summary>
    /// The confirmation, and the two questions that go with it.
    /// </summary>
    /// <remarks>
    /// The token is passed in rather than read off the route, because only its hash was ever
    /// stored: the plaintext exists on this side for the length of one request, and the handler
    /// that took it out of the path is the only thing that has it.
    ///
    /// The two do-not-add options are on this page rather than on one after it, which is a
    /// departure from the obvious ordering and worth saying why. A page after the fact would have
    /// to be authorised by something, and the token that got them here is deleted along with their
    /// participant row — so it would mean issuing a second one-shot token, mailing or redirecting
    /// to it, and accepting that whoever closes the tab first is never asked at all. Both questions
    /// fit on the form already in front of them.
    ///
    /// Neither is ticked. These are lasting refusals — nothing in this application removes a row
    /// from those lists — and a default that opts somebody out of every gift exchange they are ever
    /// invited to because they left one is not a default anybody chose.
    ///
    /// What leaving does to everybody else is stated before the button rather than after it. It is
    /// the part a reader cannot guess and the part they may want to weigh: their name goes back in
    /// the hat for everyone, not just for them.
    /// </remarks>
    public string ComposeConfirm(LeaveRoute route, string leaveToken, bool showsConsequences)
    {
        var exchange = HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(route.HatName));
        var organizerName = HttpUtility.HtmlEncode(route.Organizer.Name);
        var action = $"{Branding.LeaveUrl}/{HttpUtility.UrlEncode(leaveToken)}";

        var body = new StringBuilder();

        body.Append(
            $"""
             <p>You're about to leave {exchange}.</p>
             """);

        // Only where it is true. Somebody leaving after the exchange has been revealed is not
        // sending anybody back to the hat, and telling them they are would be asking them to weigh
        // a cost that is not there.
        body.Append(showsConsequences
            ? $"""
               <p>{organizerName} will be told that somebody has left and will need to draw names
               again, so everybody still in the exchange gets a new name. They won't be told who
               left &mdash; only you and {organizerName} will know that.</p>
               """
            : """
              <p>The exchange has already finished, so nothing changes for anybody else.</p>
              """);

        body.Append(
            $"""
             <form method="post" action="{HttpUtility.HtmlAttributeEncode(action)}">
               <p style="margin:28px 0 0;">
                 <button type="submit" style="background-color:#2f5d8a;color:#ffffff;padding:12px 22px;border:0;border-radius:4px;font-weight:bold;font-size:16px;cursor:pointer;">
                   Yes, leave this gift exchange
                 </button>
               </p>
               <p style="margin:28px 0 8px;font-weight:bold;">While you're here</p>
               <p style="margin:0 0 12px;color:#666666;font-size:14px;">Neither of these is
               required, and you can leave without either. Both last indefinitely &mdash; there's no
               link to undo them, so the way back in is for somebody to ask you first.</p>
               {Choice(BlockOrganizerField, $"Don't let {organizerName} add me to gift exchanges again")}
               {Choice(BlockAnywhereField, "Don't let anyone add me to a gift exchange")}
             </form>
             <p style="color:#666666;font-size:14px;">Nothing has happened yet. Close this page and
             you stay in the exchange.</p>
             """);

        return EmailLinkedPage.Compose("Leave this gift exchange?", body.ToString());
    }

    /// <summary>
    /// What leaving actually did, said back to them.
    /// </summary>
    /// <remarks>
    /// Lists the refusals recorded rather than reporting a count, for the reason the Ask results
    /// page lists names: these are two independent things, either can have been asked for, and a
    /// summary gives the reader no way to check that the one they meant is the one that happened.
    /// The exchange-scoped refusal is stated too, even though nobody ticked it — it is the one they
    /// would otherwise not know about.
    /// </remarks>
    public string ComposeLeft(LeaveRoute route, bool blockedOrganizer, bool blockedAnywhere)
    {
        var exchange = HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(route.HatName));
        var organizerName = HttpUtility.HtmlEncode(route.Organizer.Name);

        var body = new StringBuilder();

        body.Append(
            $"""
             <p>You've left {exchange}. Everybody else has been told that somebody left, and not who.</p>
             <p>From now on:</p>
             <ul>
               <li>You can't be added back to this gift exchange.</li>
             """);

        if (blockedOrganizer)
            body.Append($"<li>{organizerName} can't add you to any gift exchange.</li>");

        if (blockedAnywhere)
            body.Append("<li>Nobody can add you to a gift exchange.</li>");

        body.Append(
            """
            </ul>
            <p>You can close this page. There's nothing else to do.</p>
            """);

        return EmailLinkedPage.Compose("You've left", body.ToString());
    }

    /// <summary>
    /// One page for every reason leaving cannot happen.
    /// </summary>
    /// <remarks>
    /// Deliberately does not distinguish an unknown token from a spent one or from an exchange that
    /// has been deleted. Somebody holding a guessed token would otherwise learn from the difference
    /// whether it named a real participant — and here that difference is worth more than it is on
    /// the Ask, because a token that resolves is a token that removes somebody.
    ///
    /// It is also the page an organizer reaches, in the ordinary case where they have somehow got
    /// hold of a leave address. No token is ever issued for them, so nothing of theirs resolves,
    /// and this is what that looks like from outside.
    /// </remarks>
    public static string ComposeUnavailable() =>
        EmailLinkedPage.Compose(
            "This link isn't available",
            """
            <p>We can't leave a gift exchange from this link. It may have already been used, or the
            gift exchange may no longer exist.</p>
            <p>If you're still in an exchange you'd rather not be in, use the link at the bottom of
            your most recent invitation email, or reply to the person who organised it.</p>
            """);

    /// <summary>
    /// One unticked checkbox. The label is this application's own words except for the organizer's
    /// name, which the caller encodes before it arrives.
    /// </summary>
    private static string Choice(string field, string label) =>
        $"""
         <label for="{HttpUtility.HtmlAttributeEncode(field)}" style="display:block;padding:10px 12px;margin-bottom:6px;background-color:#faf8f5;border-radius:4px;cursor:pointer;">
           <input type="checkbox" id="{HttpUtility.HtmlAttributeEncode(field)}" name="{field}" value="yes" style="margin-right:10px;" />
           {label}
         </label>
         """;
}
