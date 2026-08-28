namespace GiftExchange.Library.Entities;

/// <summary>
/// What SES said happened to one message sent to one participant.
///
/// One row per outbound message, keyed on <see cref="SesMessageId"/>, rather than one per
/// participant. That id is the only thing the send and the events that follow it have in common,
/// and it is what keeps an invitation and the completion email apart — two messages to the same
/// person, which a row per participant could not distinguish.
/// </summary>
public class ParticipantEmailDeliveryEntity
{
    public required Guid ParticipantEmailDeliveryId { get; set; }

    /// <summary>
    /// Who the message went to, read from the message tag SES echoes back on every event.
    /// </summary>
    /// <remarks>
    /// No navigation property, for the reason given on <see cref="GiftIdeaEntity.ParticipantId"/>.
    /// There is a second reason here: rows are written by a function reacting to an event about a
    /// message that has already gone, so the participant it names may have been removed from the
    /// exchange in between. A foreign key would turn that ordinary race into a failed write.
    /// </remarks>
    public required Guid ParticipantId { get; set; }

    /// <summary>One of <see cref="EmailMessageType"/>.</summary>
    public required string MessageType { get; set; }

    /// <summary>
    /// The id SES assigned when it accepted the message. Unique, which
    /// <c>uq_participant_email_delivery_message</c> enforces, and the value both writers upsert on.
    /// </summary>
    public required string SesMessageId { get; set; }

    /// <summary>
    /// The furthest this message is known to have got — one of <see cref="DeliveryStatus"/>. Only
    /// ever moves forwards; see <see cref="DeliveryStatuses.RankOf"/> for why it has to.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Why, for the statuses that have a why: the bounce subtype and the receiving server's
    /// diagnostic, or the reason SES gave for refusing to send. The empty string otherwise.
    /// </summary>
    /// <remarks>
    /// This is written by remote mail servers, not by us, so it is truncated on the way in and
    /// encoded on the way out like any other text this application did not author.
    /// </remarks>
    public required string Detail { get; set; }

    /// <summary>When SES says the event happened, which is not when it reached us.</summary>
    public required DateTimeOffset OccurredAt { get; set; }

    /// <summary>When this row was last written. What separates a quiet feed from a stalled one.</summary>
    public required DateTimeOffset UpdatedAt { get; set; }
}
