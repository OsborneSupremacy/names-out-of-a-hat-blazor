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
    /// Claims one participant's Ask to another for the window, and says when the last one between
    /// the same two was if it cannot.
    /// </summary>
    /// <remarks>
    /// Held per pair rather than per asker, because the two things a limit here has to do pull in
    /// different directions. Nobody should be nagged, which is about how often one person hears
    /// from us; and somebody who asked and got nothing back should be able to try somebody else,
    /// which is about how often one person may ask. A per-asker limit serves the first and defeats
    /// the second — one silent recipient would lock the asker out of the whole exchange for a week.
    /// A pair caps what any individual receives at one request per asker per window, which is the
    /// number that actually reaches an inbox.
    ///
    /// Reports the date rather than only refusing, because what the asker is told is "we asked on
    /// the 3rd, wait a while before asking again" — and a throttle that only says no leaves the
    /// caller inventing a date it does not know.
    /// </remarks>
    /// <param name="askerParticipantId">Who is asking.</param>
    /// <param name="targetParticipantId">
    /// Who is being asked. Either the asker's own pick or another participant they hope has ideas.
    /// </param>
    /// <returns>
    /// <c>reserved</c> false when this asker already asked this person inside the window, in which
    /// case <c>previouslyAskedAt</c> is when. It is <see cref="DateTimeOffset.MinValue"/> whenever
    /// the slot was taken, and also when the previous timestamp could not be read back.
    /// </returns>
    Task<(bool reserved, DateTimeOffset previouslyAskedAt)> TryReserveAskSlotAsync(
        Guid askerParticipantId,
        Guid targetParticipantId,
        TimeSpan window);
}
