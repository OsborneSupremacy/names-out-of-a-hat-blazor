namespace GiftExchange.Library.Models;

internal record PreconditionFailureMessage
{
    public required HttpStatusCode StatusCode { get; init; }

    public required string FailureMessage { get; init; }
}

internal static class PreconditionFailureMessages
{
    public static PreconditionFailureMessage Empty = new PreconditionFailureMessage
    {
        StatusCode = HttpStatusCode.BadRequest,
        FailureMessage = string.Empty
    };
}
