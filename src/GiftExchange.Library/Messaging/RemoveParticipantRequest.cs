namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record RemoveParticipantRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    public required Guid PersonId { get; init; }
}
