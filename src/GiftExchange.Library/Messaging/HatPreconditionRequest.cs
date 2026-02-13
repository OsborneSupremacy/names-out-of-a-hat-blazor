namespace GiftExchange.Library.Messaging;

internal record HatPreconditionRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    public required Dictionary<string, string> FieldsToModerate { get; init; }

    public required ImmutableList<string> ValidHatStatuses { get; init; }
}
