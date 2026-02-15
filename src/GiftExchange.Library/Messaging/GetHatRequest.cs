namespace GiftExchange.Library.Messaging;

public record GetHatRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }
}
