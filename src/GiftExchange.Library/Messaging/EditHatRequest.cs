namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record EditHatRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    public required string Name { get; init; }

    public required string AdditionalInformation { get; init; }

    public required string PriceRange { get; init; }
}
