namespace GiftExchange.Library.Abstractions;

/// <summary>
/// Caps how often this application will act on a repeated request from the same source.
/// </summary>
public interface IReplyThrottleProvider
{
    /// <summary>Claims this address's one reply for the window.</summary>
    /// <returns>false when it has already been answered inside the window.</returns>
    Task<bool> TryReserveReplySlotAsync(string kind, string email);

    /// <summary>
    /// Claims this participant's one Ask for the window, and says when the last one was if it
    /// cannot.
    /// </summary>
    /// <remarks>
    /// Reports the date rather than only refusing, because what the asker is told is "we asked on
    /// the 3rd, wait a while before asking again" — and a throttle that only says no leaves the
    /// caller inventing a date it does not know.
    /// </remarks>
    /// <returns>
    /// <c>reserved</c> false when an Ask was already made inside the window, in which case
    /// <c>previouslyAskedAt</c> is when. It is <see cref="DateTimeOffset.MinValue"/> whenever the
    /// slot was taken, and also when the previous timestamp could not be read back.
    /// </returns>
    Task<(bool reserved, DateTimeOffset previouslyAskedAt)> TryReserveAskSlotAsync(
        Guid askerParticipantId,
        TimeSpan window);
}
