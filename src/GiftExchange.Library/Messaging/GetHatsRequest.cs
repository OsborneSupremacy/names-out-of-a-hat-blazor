namespace GiftExchange.Library.Messaging;

public record GetHatsRequest
{
    public required string OrganizerEmail { get; init; }
}
