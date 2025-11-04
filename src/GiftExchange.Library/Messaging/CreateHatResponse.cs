namespace GiftExchange.Library.Messaging;

public record CreateHatResponse
{
    public required Guid HatId { get; init; }
}
