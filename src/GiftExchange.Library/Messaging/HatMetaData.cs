namespace GiftExchange.Library.Messaging;

public record HatMetaData
{
    public required Guid HatId { get; init; }

    public required string HatName { get; init; }

    public required string Status { get; init; }

    public required bool InvitationsQueued { get; init; }
}
