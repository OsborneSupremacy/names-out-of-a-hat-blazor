namespace GiftExchange.Library.Messaging;

public record HatMetaData
{
    public required Guid HatId { get; init; }

    public required string HatName { get; init; }

    public required string Status { get; init; }

    /// <summary>
    /// When <see cref="Status"/> last changed, so the list can say how long an exchange has sat
    /// where it is rather than only what it says.
    /// </summary>
    public required DateTimeOffset StatusUpdatedAt { get; init; }
}
