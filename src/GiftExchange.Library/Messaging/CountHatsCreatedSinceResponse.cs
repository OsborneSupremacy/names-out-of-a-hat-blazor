namespace GiftExchange.Library.Messaging;

/// <summary>
/// What the organizer has created inside the window: how many, and when the first of them was made.
/// </summary>
/// <remarks>
/// The earliest timestamp comes back with the count because a rolling window has no "tomorrow" to
/// point at. The oldest exchange inside it is the one that falls out first, so it is the only thing
/// that can tell somebody who has been refused when they may try again.
/// </remarks>
internal record CountHatsCreatedSinceResponse
{
    public required int Count { get; init; }

    /// <summary>
    /// When the oldest exchange inside the window was created, or <see cref="DateTimeOffset.MinValue"/>
    /// when there are none.
    /// </summary>
    public required DateTimeOffset EarliestCreatedAt { get; init; }
}
