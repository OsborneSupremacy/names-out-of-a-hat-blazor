namespace GiftExchange.Library.Messaging;

internal record HatPreconditionResponse
{
    public required bool PreconditionsMet { get; init; }

    public required PreconditionFailureMessage PreconditionFailureMessage { get; init; }

    public required Hat Hat { get; init; }
}
