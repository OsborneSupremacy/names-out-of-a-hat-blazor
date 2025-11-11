namespace GiftExchange.Library.Messaging;

public record VerifyOrganizerRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerName { get; init; }
}
