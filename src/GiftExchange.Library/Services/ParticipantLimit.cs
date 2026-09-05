namespace GiftExchange.Library.Services;

/// <summary>
/// How many people one gift exchange may hold, and what an organizer is told when they reach it.
/// </summary>
/// <remarks>
/// The number is not a guess at what feels like enough. Eligibility is stored a pair at a time, so
/// a hat of n people carries up to n(n-1) rows beside its n participants, and the two operations
/// that touch all of them -- copying an exchange and deleting one -- do their whole job inside a
/// single transaction. Aurora DSQL modifies at most 3,000 rows in one, which puts a real ceiling
/// under this application at around 54 people: a copy of 55 writes 3,026 rows before anything else
/// is counted, and a delete of 55 has the tokens, delivery records and gift ideas to remove on top.
/// Fifty leaves the copy at 2,501 and the delete with room for the rest of what it sweeps up.
///
/// That ceiling and the product answer happen to agree, which is the only reason a number this
/// convenient is being used. Fifty is already an office rather than a family or a team, and an
/// exchange that size is one nobody would have run out of a hat either.
///
/// If more is ever wanted, the number is not the thing to change. Batching the copy and the delete
/// across several transactions is, and this constant can follow it up afterwards.
/// </remarks>
internal static class ParticipantLimit
{
    /// <summary>People one exchange may hold, the organizer included.</summary>
    internal const int MaxParticipants = 50;

    /// <summary>
    /// Says what the exchange is up against and the one thing that makes room, without pretending
    /// there is a way to raise it.
    /// </summary>
    internal static readonly string RefusalMessage =
        $"This gift exchange already has {MaxParticipants} participants, which is as many as this application allows. "
        + "Remove somebody to make room, or run a second exchange alongside it.";

    /// <summary>
    /// Told to an organizer copying an exchange that is bigger than a new one is allowed to be.
    /// </summary>
    /// <remarks>
    /// Only exchanges that predate <see cref="MaxParticipants"/> can be this large, so this is the
    /// message for a situation nothing can create any more. It is worth having anyway: the copy is
    /// written in one transaction, and without this the organizer would meet a database error
    /// instead of a sentence telling them what to do about it.
    /// </remarks>
    internal static readonly string CopyRefusalMessage =
        $"This gift exchange has more than the {MaxParticipants} participants a new one may hold, so it cannot be copied. "
        + "Remove somebody from it first, or start the new exchange from scratch.";
}
