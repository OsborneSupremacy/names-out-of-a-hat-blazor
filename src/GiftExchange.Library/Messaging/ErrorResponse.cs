namespace GiftExchange.Library.Messaging;

internal record ErrorResponse
{
    public required string[] Errors { get; init; }
}
