namespace GiftExchange.Library.Messaging;

public record ValidateHatRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }
}
