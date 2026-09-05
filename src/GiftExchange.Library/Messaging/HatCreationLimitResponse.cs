namespace GiftExchange.Library.Messaging;

/// <summary>
/// The limiter's answer to "may this organizer create another exchange right now?"
/// </summary>
internal record HatCreationLimitResponse
{
    public required bool WithinLimit { get; init; }

    /// <summary>
    /// The soonest the refused organizer may try again — the moment the oldest exchange in the
    /// window falls out of it. <see cref="DateTimeOffset.MinValue"/> whenever they are within the
    /// limit.
    /// </summary>
    public required DateTimeOffset NextAllowedAt { get; init; }
}
