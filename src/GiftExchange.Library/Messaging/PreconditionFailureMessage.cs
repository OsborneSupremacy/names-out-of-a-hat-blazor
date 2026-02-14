namespace GiftExchange.Library.Messaging;

internal record PreconditionFailureMessage
{
    public required HttpStatusCode StatusCode { get; init; }

    public required string FailureMessage { get; init; }
}

internal static class PreconditionFailureMessages
{
    public static readonly PreconditionFailureMessage Empty = new PreconditionFailureMessage
    {
        StatusCode = HttpStatusCode.BadRequest,
        FailureMessage = string.Empty
    };
}
