namespace GiftExchange.Library.Messaging;

/// <summary>
/// The addresses to test against the three do-not-add lists, and the scope to test them in.
/// </summary>
/// <remarks>
/// A list rather than a single address because one caller checks many at once: copying a finished
/// exchange re-adds everybody in it, and asking three questions per participant in a loop would be
/// three round trips a head for something that answers in three regardless.
/// </remarks>
[UsedImplicitly]
internal record DoNotAddCheckRequest
{
    /// <summary>The addresses being added. Normalized by the service, not by the caller.</summary>
    public required ImmutableList<string> Emails { get; init; }

    /// <summary>Whoever is doing the adding.</summary>
    public required string OrganizerEmail { get; init; }

    /// <summary>
    /// The exchange whose own list applies.
    /// </summary>
    /// <remarks>
    /// For an ordinary add that is the exchange being added to. For a copy it is the <em>source</em>
    /// exchange: the refusals were written against the one somebody left, and the new hat has no
    /// list of its own yet.
    /// </remarks>
    public required Guid HatId { get; init; }
}
