namespace GiftExchange.Library.Messaging;

/// <summary>
/// The message tags every participant email is sent with, and that its SES events carry back.
///
/// SES keeps nothing of ours. An event names a message id, a destination address and whatever tags
/// the send attached, so a tag is the only way an event can say which participant it is about —
/// and the destination address will not do, because in test mode every message goes to the same
/// inbox, and because one address can be in several exchanges at once.
/// </summary>
/// <remarks>
/// Tag names and values are restricted by SES to ASCII letters, digits, dashes and underscores.
/// Both names here comply, and so do the values: a <see cref="Guid"/> formatted the default way is
/// hex and dashes, and the message types are single uppercase words.
/// </remarks>
internal static class SesMessageTags
{
    /// <summary>The participant the message was addressed to.</summary>
    public const string ParticipantId = "participant_id";

    /// <summary>One of <see cref="EmailMessageType"/>.</summary>
    public const string MessageType = "message_type";

    /// <summary>
    /// Reads a single-valued tag from what SES echoed back.
    /// </summary>
    /// <remarks>
    /// Tags are multi-valued in the API and every one this application sets has exactly one value,
    /// so the first is the value. An absent tag is the empty string rather than an error: the
    /// configuration set is on the sending identity, so a message sent by some future code path
    /// that does not tag its sends still produces events, and those should be logged and dropped
    /// rather than jammed at the head of the queue.
    /// </remarks>
    public static string Read(SesEventMail mail, string name) =>
        mail.Tags.TryGetValue(name, out var values) && values.Count > 0
            ? values[0] ?? string.Empty
            : string.Empty;
}
