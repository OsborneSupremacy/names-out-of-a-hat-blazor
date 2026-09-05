namespace GiftExchange.Library.Messaging;

/// <summary>
/// A question put to the data layer on behalf of <c>HatCreationLimiter</c>: how much of their daily
/// allowance has this organizer already spent?
/// </summary>
/// <remarks>
/// The moment to count from is passed in rather than decided here, so the window lives in one place
/// — the limiter — and the provider only answers what it is asked.
/// </remarks>
internal record CountHatsCreatedSinceRequest
{
    public required string OrganizerEmail { get; init; }

    /// <summary>Exchanges created at or after this moment are counted; older ones are not.</summary>
    public required DateTimeOffset Since { get; init; }
}
