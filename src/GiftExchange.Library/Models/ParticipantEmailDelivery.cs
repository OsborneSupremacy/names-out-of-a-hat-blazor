namespace GiftExchange.Library.Models;

/// <summary>
/// One thing SES said about one message, on its way to being written down.
///
/// A domain record rather than the entity, so that the service turning SES's wire format into
/// something storable does not also have to know how it is stored. It carries no id: the row is
/// found by <see cref="SesMessageId"/>, and whether one exists yet is the provider's business.
/// </summary>
public record ParticipantEmailDelivery
{
    public required Guid ParticipantId { get; init; }

    /// <summary>One of <see cref="EmailMessageType"/>.</summary>
    public required string MessageType { get; init; }

    public required string SesMessageId { get; init; }

    /// <summary>One of <see cref="Models.DeliveryStatus"/>.</summary>
    public required string Status { get; init; }

    /// <summary>
    /// Why, for the statuses that have a why. Already truncated to what the column holds by the
    /// time it gets here.
    /// </summary>
    public required string Detail { get; init; }

    /// <summary>When SES says it happened, which is not when it reached us.</summary>
    public required DateTimeOffset OccurredAt { get; init; }
}
