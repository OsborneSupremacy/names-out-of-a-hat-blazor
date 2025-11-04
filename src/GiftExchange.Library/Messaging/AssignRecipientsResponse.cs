namespace GiftExchange.Library.Messaging;

public record AssignRecipientsResponse
{
    public required string SuccessMessage { get; init; }

    public required ImmutableList<string> Errors { get; init; }
}
