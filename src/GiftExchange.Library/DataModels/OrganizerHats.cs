namespace GiftExchange.Library.DataModels;

internal record OrganizerHats
{
    public required string OrganizerEmail { get; init; }

    public required ImmutableList<Guid> HatIds { get; init; }
}
