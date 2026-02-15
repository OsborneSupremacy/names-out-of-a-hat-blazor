namespace GiftExchange.Library.Messaging;

internal record CloseHatRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }
}
