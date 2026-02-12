namespace GiftExchange.Library.DataModels;

public record HatDataModel
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    public required string OrganizerName { get; init; }

    public required string HatName { get; init; }

    public required string Status { get; init; }

    public required string AdditionalInformation { get; init; }

    public required string PriceRange { get; init; }

    public required bool OrganizerVerified { get; init; }

    public required bool RecipientsAssigned { get; init; }
}
