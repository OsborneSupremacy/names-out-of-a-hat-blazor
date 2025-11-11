namespace GiftExchange.Library.Messaging;

public record SendInvitationsRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerName { get; init; }
}
