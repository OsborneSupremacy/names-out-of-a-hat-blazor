namespace GiftExchange.Library.Services;

/// <summary>
/// A cap on how many gift exchanges one organizer can start in a day, shared by the two paths that
/// start one: creating from scratch and copying a finished exchange.
/// </summary>
/// <remarks>
/// A copy counts against the same allowance as a fresh exchange. It has to: it writes a hat and a
/// full set of participants, so exempting it would leave the limit with a door next to it.
///
/// Worth being honest about what this is. Creating an exchange sends no mail and needs a signed-in
/// organizer, so this is not the thing standing between the application and a spam run — the send
/// path is. What it bounds is how much one account can pile into the database in a sitting, whether
/// by a script or by a stuck client retrying, and it does that at a height no legitimate organizer
/// should ever reach: five is more exchanges than a person runs in a season, let alone in a day.
///
/// The window rolls rather than resetting at midnight, which avoids having to decide whose midnight
/// it is. The cost is that "try again tomorrow" would be a lie, so the refusal names a time instead.
/// </remarks>
internal class HatCreationLimiter
{
    /// <summary>Exchanges one organizer may start inside <see cref="Window"/>.</summary>
    internal const int DailyLimit = 5;

    private static readonly TimeSpan Window = TimeSpan.FromHours(24);

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly ILogger<HatCreationLimiter> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public HatCreationLimiter(GiftExchangeProvider giftExchangeProvider, ILogger<HatCreationLimiter> logger)
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Whether this organizer may start another exchange right now.</summary>
    /// <remarks>
    /// Two requests arriving together can both read the same count and both be allowed, so the real
    /// ceiling is the limit plus however many are in flight. That is left alone deliberately: the
    /// alternative is a reservation to serialize creation, and the difference between five and six
    /// does not justify it when the point is to stop hundreds.
    /// </remarks>
    public async Task<HatCreationLimitResponse> CheckAsync(string organizerEmail)
    {
        var created = await _giftExchangeProvider
            .CountHatsCreatedSinceAsync(new CountHatsCreatedSinceRequest
            {
                OrganizerEmail = organizerEmail,
                Since = DateTimeOffset.UtcNow.Subtract(Window)
            })
            .ConfigureAwait(false);

        if (created.Count < DailyLimit)
            return new HatCreationLimitResponse
            {
                WithinLimit = true,
                NextAllowedAt = DateTimeOffset.MinValue
            };

        _logger.LogWarning(
            "An organizer has reached the daily limit of {DailyLimit} gift exchanges; refusing another.",
            DailyLimit);

        return new HatCreationLimitResponse
        {
            WithinLimit = false,
            // The oldest one inside the window is the one that leaves it first, so its creation
            // time plus the window is the moment an allowance opens up again.
            NextAllowedAt = created.EarliestCreatedAt.Add(Window)
        };
    }

    /// <summary>
    /// Names when the organizer may try again, and the one thing they can do about it sooner.
    /// </summary>
    /// <remarks>
    /// Deleting is offered because the count is taken from the exchanges they own, so it genuinely
    /// works — see <c>GiftExchangeProvider.CountHatsCreatedSinceAsync</c>. Somebody who hit the
    /// limit making the same exchange five times over should not have to wait a day to fix it.
    /// </remarks>
    internal static string RefusalMessage(DateTimeOffset nextAllowedAt) =>
        $"You have started {DailyLimit} gift exchanges in the past day, which is as many as this application allows. "
        + $"You can start another after {nextAllowedAt:HH:mm} UTC, or sooner if you delete one you no longer need.";
}
