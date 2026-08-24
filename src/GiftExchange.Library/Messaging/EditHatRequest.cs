namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record EditHatRequest : IOrganizerScopedRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    public required string Name { get; init; }

    public required string AdditionalInformation { get; init; }

    public required string PriceRange { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
