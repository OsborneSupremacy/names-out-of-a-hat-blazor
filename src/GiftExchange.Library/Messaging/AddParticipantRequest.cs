namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record AddParticipantRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }
}
