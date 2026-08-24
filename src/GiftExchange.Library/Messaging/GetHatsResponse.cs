namespace GiftExchange.Library.Messaging;

public record GetHatsResponse
{
    /// <summary>
    /// The name this organizer is known by, read back from their existing hats. Empty for someone
    /// who has not created one yet, which is the only case where the UI has to ask.
    /// </summary>
    public required string OrganizerName { get; init; }

    public required ImmutableList<HatMetaData> Hats { get; init; }
}
