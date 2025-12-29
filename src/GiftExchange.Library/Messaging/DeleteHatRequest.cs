namespace GiftExchange.Library.Messaging;

public record DeleteHatRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }
}
