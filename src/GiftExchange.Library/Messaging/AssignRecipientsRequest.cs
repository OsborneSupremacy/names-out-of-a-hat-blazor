namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record AssignRecipientsRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }
}
