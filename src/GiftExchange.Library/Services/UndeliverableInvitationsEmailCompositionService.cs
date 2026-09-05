using System.Web;

namespace GiftExchange.Library.Services;

/// <summary>
/// The message an organizer gets when some of the invitations they sent did not arrive.
/// </summary>
/// <remarks>
/// The delivery column already says all of this on screen, and that is exactly the problem it
/// exists to solve: an organizer sends invitations and closes the tab. Nothing brings them back to
/// look, so a bounce sits there unread until somebody at the exchange says they never got a name --
/// by which point the shopping is done. This is the one push in an otherwise pull-only feature.
///
/// It is written to the organizer alone, and it names participants and quotes their addresses. That
/// is safe here in a way it would not be in any other message this application sends: the organizer
/// typed these addresses in, so nothing in this email is a fact they did not already hold. Nothing
/// about the draw is in it, which matters -- an organizer who is also a participant must not learn
/// from an administrative notice something the invitation kept from them.
///
/// Sent by <see cref="AutomaticEmailSender"/> rather than through the invitation queue, so it
/// carries no configuration set and generates no delivery events of its own. Tagging it against the
/// organizer's participant row would overwrite the very status this email is about, replacing "your
/// invitation bounced" with "the notice telling you your invitation bounced was delivered".
/// </remarks>
[UsedImplicitly]
internal class UndeliverableInvitationsEmailCompositionService
{
    /// <summary>
    /// Plain text, like every subject here, and deliberately not alarming.
    /// </summary>
    /// <remarks>
    /// It says how many, because the count is the whole of the decision about whether to open this
    /// now or after dinner, and a subject that made an organizer open the email to learn it would
    /// be wasting the one line they are guaranteed to read.
    /// </remarks>
    internal static string GetSubject(ComposeUndeliverableNoticeRequest request) =>
        request.Undeliverable.Count == 1
            ? $"An invitation didn't arrive: {GiftExchangeNaming.Describe(request.Hat.Name)}"
            : $"{request.Undeliverable.Count} invitations didn't arrive: {GiftExchangeNaming.Describe(request.Hat.Name)}";

    /// <summary>
    /// What went wrong, who it happened to, and the one thing that fixes it.
    /// </summary>
    /// <remarks>
    /// In that order, and the order is the point. An organizer reading this on a phone should be
    /// able to stop after the first line and still know whether it concerns them.
    ///
    /// The claim is kept narrow on purpose. It says these did not arrive, and says nothing at all
    /// about anybody else -- because a participant nothing has been heard about is not a
    /// participant who missed their invitation, and an email that implied otherwise would send an
    /// organizer chasing people who are holding theirs. <see cref="DeliveryStatuses.Undeliverable"/>
    /// is where that line is drawn.
    /// </remarks>
    internal string ComposeEmail(ComposeUndeliverableNoticeRequest request)
    {
        var hat = request.Hat;
        var organizerName = HttpUtility.HtmlEncode(hat.Organizer.Name);
        var count = request.Undeliverable.Count;

        var lines = new List<string>
        {
            EmailBranding.Masthead(),
            $"Dear {organizerName},",
            count == 1
                ? $"One of the invitations you sent for {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hat.Name))} could not be delivered."
                : $"{count} of the invitations you sent for {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hat.Name))} could not be delivered.",
            BuildList(request.Undeliverable),
            // Two sentences, because the first is the fact and the second is the action. The
            // distinction between a wrong address and a working one that refused the message is
            // left to the reasons quoted above rather than guessed at here -- we only know what the
            // far end said.
            count == 1
                ? "Please check that address. A typo is the usual explanation, and the reason given above will often say so outright."
                : "Please check those addresses. A typo is the usual explanation, and the reasons given above will often say so outright.",
            BuildAction(hat),
            // Said plainly because the alternative is an organizer assuming the worst about
            // everybody. This email is about the addresses named in it and no others.
            "<i>Everybody else's invitation is unaffected. This email only lists the ones we know did not arrive.</i>",
            BuildSmallPrint()
        };

        return string.Join("<br /><br />", lines) + "<br /><br />";
    }

    /// <summary>
    /// One row per failure: who, at what address, and what the far end said.
    /// </summary>
    /// <remarks>
    /// The name and the address together, because the organizer has to match a row to a person they
    /// know before they can tell whether the address is wrong, and because the address as typed is
    /// the thing they are being asked to look at.
    ///
    /// The reason underneath is the receiving mail server's own sentence, which is usually the only
    /// part of this that names the actual problem -- and it is the one piece of text in this email
    /// that no part of this application wrote. It is encoded like everything else, and rendered as
    /// a quotation so it reads as somebody else's words rather than as ours.
    ///
    /// The date is there for the same reason it is in the delivery column: it is what lets an
    /// organizer tell this failure from one they already dealt with this morning.
    /// </remarks>
    private static string BuildList(ImmutableList<Participant> undeliverable)
    {
        var rows = undeliverable
            .Select(participant =>
            {
                var row = new StringBuilder();

                row.Append($"{participant.Emoji} <b>{HttpUtility.HtmlEncode(participant.Person.Name)}</b>");
                row.Append($" &mdash; {HttpUtility.HtmlEncode(participant.Person.Email)}");
                row.Append($"<br /><small style=\"color:#666666;\">{HttpUtility.HtmlEncode(Explain(participant))}</small>");

                return row.ToString();
            });

        return $"""
                <div style="border-left:3px solid #cccccc;padding-left:12px;color:#333333;">
                {string.Join("<br /><br />", rows)}
                </div>
                """;
    }

    /// <summary>
    /// One sentence saying what happened to this participant's invitation and when.
    /// </summary>
    /// <remarks>
    /// Worded rather than reported as a status word. "BOUNCED" is this application's vocabulary and
    /// means nothing to somebody reading their email; what they need is that it came back and that
    /// the address is the thing to look at.
    ///
    /// The detail is appended only when there is one. A rendering failure has an error message that
    /// is about us rather than about them, and a bounce sometimes arrives with nothing attached at
    /// all -- in both cases a trailing empty quotation would read as though something had been lost.
    /// </remarks>
    private static string Explain(Participant participant)
    {
        var when = participant.DeliveryOccurredAt == DateTimeOffset.MinValue
            ? string.Empty
            : $" on {participant.DeliveryOccurredAt.UtcDateTime:d MMMM} at {participant.DeliveryOccurredAt.UtcDateTime:HH:mm} UTC";

        var what = participant.DeliveryStatus switch
        {
            var status when status == DeliveryStatus.Bounced => $"Came back undelivered{when}.",
            var status when status == DeliveryStatus.Rejected => $"We were not able to send this one{when}.",
            var status when status == DeliveryStatus.Failed => $"We were not able to send this one{when}.",
            // Nothing routes anything else here. Kept general rather than omitted, so that a status
            // added to DeliveryStatuses.Undeliverable later produces a sentence rather than a blank.
            _ => $"Did not arrive{when}."
        };

        return string.IsNullOrWhiteSpace(participant.DeliveryDetail)
            ? what
            : $"{what} The mail system said: {participant.DeliveryDetail}";
    }

    /// <summary>
    /// Where to go, and what they will find when they get there.
    /// </summary>
    /// <remarks>
    /// Named as a correction rather than as an edit, because the two are different buttons in the
    /// application and only one of them is safe here: editing a participant sends the exchange back
    /// to IN_PROGRESS and throws the draw away, while correcting an address leaves the draw alone
    /// and resends to that person by itself. Sending an organizer to the wrong one would undo the
    /// exchange they are trying to repair.
    /// </remarks>
    private static string BuildAction(Hat hat) =>
        $"""
         <a href="{HttpUtility.HtmlAttributeEncode(Branding.HatUrl(hat.Id))}">Open {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hat.Name))}</a> and use <b>Correct address</b> beside anybody listed above. Correcting an address sends that person their invitation again straight away, and leaves everybody else's name exactly as it was drawn.
         """;

    /// <summary>
    /// Shorter still than the others, and the one footer here that does not say it was sent on
    /// somebody's behalf: this email is not from an organizer, it is to one.
    /// </summary>
    private static string BuildSmallPrint() =>
        $"""
         <small style="color:#666666;">
         You're getting this because you sent invitations for a gift exchange at <a href="{Branding.SiteUrl}">namesoutofahat.com</a>. It's sent once, a couple of hours after invitations go out, and only when something didn't arrive.
         <br /><br />
         Nobody reads replies to this address.
         </small>
         """;
}
