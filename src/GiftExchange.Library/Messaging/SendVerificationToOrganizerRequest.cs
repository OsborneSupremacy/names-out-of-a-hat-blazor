namespace GiftExchange.Library.Messaging;

public record SendVerificationToOrganizerRequest
{
    public required Guid HatId { get; init; }
}
