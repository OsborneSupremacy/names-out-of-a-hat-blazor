namespace GiftExchange.Library.Models;

/// <summary>
/// Which of the things this application sends to a participant a message was.
///
/// Carried on the send as an SES message tag and written back onto the delivery row, because
/// without it the two are indistinguishable once they are in the table: both are "an email to this
/// participant", and an organizer looking at a closed exchange would have no way to tell a bounced
/// invitation from a bounced announcement that the exchange had finished.
/// </summary>
public static class EmailMessageType
{
    public static string Invitation => "INVITATION";

    public static string Completion => "COMPLETION";

    /// <summary>
    /// The notice sent to everybody still in an exchange after somebody leaves it. Names nobody.
    /// </summary>
    public static string ParticipantLeft => "PARTICIPANT_LEFT";

    /// <summary>
    /// The notice sent to the organizer alone after somebody leaves, which does name them. Tagged
    /// apart from <see cref="ParticipantLeft"/> because an organizer who is also a participant
    /// receives both, and a delivery row that could not tell them apart would report one bounce for
    /// two different messages.
    /// </summary>
    /// <remarks>
    /// Spelled shorter than the property that holds it, and deliberately so. This value is written
    /// verbatim into <c>participant_email_delivery.message_type</c>, which is twenty characters and
    /// cannot grow — DSQL has no ALTER COLUMN, so the column is whatever its CREATE TABLE said.
    /// ORGANIZER_PARTICIPANT_LEFT was twenty-six, and every delivery event for one of these notices
    /// failed its insert until this was cut down.
    /// </remarks>
    public static string OrganizerParticipantLeft => "ORGANIZER_LEFT_NOTE";

    /// <summary>
    /// A message that carried no type tag. Not written by anything here — it exists so that a send
    /// added later, by somebody who forgets the tag, records a row rather than being dropped.
    /// </summary>
    public static string Unspecified => "UNSPECIFIED";
}

public static class EmailMessageTypes
{
    /// <summary>
    /// The types a send may tag itself with. <see cref="EmailMessageType.Unspecified"/> is not
    /// among them: it is what an untagged message becomes, never something to tag with.
    /// </summary>
    public static readonly ImmutableList<string> All =
    [
        EmailMessageType.Invitation,
        EmailMessageType.Completion,
        EmailMessageType.ParticipantLeft,
        EmailMessageType.OrganizerParticipantLeft
    ];
}
