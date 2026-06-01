namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record PreviewInvitationsRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }
}
