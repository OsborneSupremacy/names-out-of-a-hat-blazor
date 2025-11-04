namespace GiftExchange.Library.Messaging;

public record AddParticipantRequest
{
    public required Guid HatId { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }
}
