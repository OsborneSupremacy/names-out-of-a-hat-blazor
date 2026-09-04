using System.Web;

namespace GiftExchange.Library.Services;

/// <summary>
/// The two messages that go out when somebody leaves a gift exchange.
/// </summary>
/// <remarks>
/// One is sent to everybody still in the exchange and names nobody; the other goes to the organizer
/// and names the person outright. Written in one class because the pair only make sense read
/// together — the whole design of the first is a consequence of what the second is allowed to say —
/// and because a change to one that is not made to the other is how the name leaks.
///
/// What the participants' message must not do is let anybody work out who left. It therefore does
/// not name them, does not say how many are left, and is sent to every remaining participant
/// identically, including the organizer. An organizer who is also a participant receives both this
/// and the named one, which is correct: the first is what their exchange was told, and the second
/// is what they need in order to act.
///
/// Neither is a reply to anything, so neither invites one. Both go through the ordinary invitation
/// queue rather than the automatic sender, so that a bounce or a complaint is recorded against the
/// participant the way an invitation's is.
/// </remarks>
[UsedImplicitly]
public class LeaveEmailCompositionService
{
    /// <summary>
    /// What everybody else is told. Plain text, like every subject here.
    /// </summary>
    /// <remarks>
    /// Says an update rather than that somebody left. A subject line is shown in a preview pane
    /// next to a sender, and "somebody has left the gift exchange" sitting unread in an inbox is a
    /// fact about the sender's exchange that this application has no business broadcasting to
    /// whoever is looking over their shoulder. The body says it plainly enough.
    /// </remarks>
    public static string GetParticipantSubject(Hat hat) =>
        $"An update about {GiftExchangeNaming.Describe(hat.Name)}";

    public static string GetOrganizerSubject(Hat hat) =>
        $"Somebody has left {GiftExchangeNaming.Describe(hat.Name)}";

    /// <summary>
    /// The message to everybody still in the exchange.
    /// </summary>
    /// <remarks>
    /// Three things, in the order somebody needs them: the name they were given is void, the draw
    /// is going to happen again, and it is the organizer who has to do it. The last is there so
    /// that nobody sits waiting on this application to fix something only a person can.
    ///
    /// It does not say who left, how many people are left, or when the new draw will be. The first
    /// is the point. The second is the same fact by arithmetic, for anybody who counted. The third
    /// is not ours to promise.
    /// </remarks>
    public string ComposeParticipantNotice(Hat hat)
    {
        var organizerName = HttpUtility.HtmlEncode(hat.Organizer.Name);
        var organizerEmail = HttpUtility.HtmlEncode(hat.Organizer.Email);

        var lines = new List<string>
        {
            EmailBranding.Masthead(),
            $"Somebody has asked to leave {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hat.Name))}.",
            "<b>Please disregard the name you were assigned.</b> It no longer applies.",
            $"{organizerName} will need to shake the hat again, and everybody will be sent a new name. You don't need to do anything until that arrives.",
            $"""If you have any questions, contact <a href="mailto:{HttpUtility.HtmlAttributeEncode(hat.Organizer.Email)}">{organizerName}</a>.""",
            "<i>We're not saying who left, and please don't ask around. Working it out between you is the one thing that would make this worse for them.</i>",
            BuildSmallPrint(organizerEmail)
        };

        return Join(lines);
    }

    /// <summary>
    /// The message to the organizer, which does name them.
    /// </summary>
    /// <remarks>
    /// The organizer is told who, because they cannot run the exchange otherwise: they have to know
    /// not to chase the person, not to add them back, and who to speak to if this was a
    /// misunderstanding. They are the one person with a reason to know and, being the one who added
    /// them, the one person who already had their address.
    ///
    /// The advice at the end is the point of the email as much as the news is. Nearly every leave
    /// traces back to somebody being entered into an exchange without being asked, and an organizer
    /// who reads this and asks first next time is the only fix that scales. It is worded as a
    /// suggestion rather than a rebuke — they have just lost a participant, and being told off is
    /// not what makes anybody read the next sentence.
    /// </remarks>
    public string ComposeOrganizerNotice(Hat hat, Person leaver, bool namesMustBeDrawnAgain)
    {
        var leaverName = HttpUtility.HtmlEncode(leaver.Name);
        var organizerEmail = HttpUtility.HtmlEncode(hat.Organizer.Email);

        var lines = new List<string>
        {
            EmailBranding.Masthead(),
            $"Dear {HttpUtility.HtmlEncode(hat.Organizer.Name)},",
            $"""{leaverName} (<a href="mailto:{HttpUtility.HtmlAttributeEncode(leaver.Email)}">{HttpUtility.HtmlEncode(leaver.Email)}</a>) has left {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hat.Name))}.""",
            "They've been removed, and they can't be added to this gift exchange again."
        };

        // Only where it is true. An exchange that has already been revealed is not going back to
        // the hat, and telling an organizer to draw names again for one that is over would send
        // them looking for a button that should not be there.
        lines.Add(namesMustBeDrawnAgain
            ? "Everybody else has been told that somebody left, without being told who. The gift exchange has gone back to <b>in progress</b>, so you'll need to shake the hat again and send out new invitations. Nobody should act on the name they were given before."
            : "The gift exchange had already finished, so nothing else has changed and nobody else has been told.");

        lines.AddRange([
            "<i>Worth doing next time: check with somebody before you add them to a gift exchange. Almost everybody who leaves does it because the first they heard of it was the invitation, and a quick word beforehand saves everyone the redraw.</i>",
            BuildSmallPrint(organizerEmail)
        ]);

        return Join(lines);
    }

    private static string Join(List<string> lines)
    {
        var body = new StringBuilder();

        foreach (var line in lines)
        {
            body.Append(line);
            body.AppendLine("<br /><br />");
        }

        return body.ToString();
    }

    /// <summary>
    /// The completion email's footer rather than the invitation's, and for its reason: nothing in
    /// either of these messages is the organizer's own words except their name and the exchange's,
    /// so the disclaimer the invitation carries has nothing to disclaim here.
    /// </summary>
    private static string BuildSmallPrint(string organizerEmail) =>
        $"""
         <small style="color:#666666;">
         This email was sent on behalf of {organizerEmail} through <a href="{Branding.SiteUrl}">namesoutofahat.com</a>, a free app for running gift exchanges where names are drawn at random.
         <br /><br />
         Nobody reads replies to this address, so please contact the organizer directly if you need to.
         </small>
         """;
}
