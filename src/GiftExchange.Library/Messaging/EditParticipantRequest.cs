namespace GiftExchange.Library.Messaging;

public record EditParticipantRequest
{
    public required Guid HatId { get; init; }

    public required Guid PersonId { get; init; }

    public required ImmutableList<Recipient> Recipients { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }
}
