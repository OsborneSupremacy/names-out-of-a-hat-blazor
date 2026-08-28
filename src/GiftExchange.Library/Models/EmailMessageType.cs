namespace GiftExchange.Library.Models;

/// <summary>
/// Which of the two things this application sends to everybody at once a message was.
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
        EmailMessageType.Completion
    ];
}
