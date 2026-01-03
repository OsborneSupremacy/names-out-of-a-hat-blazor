namespace GiftExchange.Library.Messaging;

/// <summary>
/// This is consistent with API Gateway's default error response structure.
/// </summary>
internal record ErrorResponse
{
    public required string Message { get; init; }
}
