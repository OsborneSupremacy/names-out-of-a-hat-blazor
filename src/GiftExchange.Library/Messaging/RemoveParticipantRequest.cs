namespace GiftExchange.Library.Messaging;

public record RemoveParticipantRequest
{
    public required Guid HatId { get; init; }

    public required Guid PersonId { get; init; }
}
