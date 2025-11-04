namespace GiftExchange.Library.Messaging;

public record AssignRecipientsRequest
{
    public required Guid HatId { get; init; }
}
