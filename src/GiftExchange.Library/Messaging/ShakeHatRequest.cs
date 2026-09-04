namespace GiftExchange.Library.Messaging;

/// <summary>
/// One run of the shaker: who is in the hat, what shape the draw has to come out in, and how many
/// times to try before giving up.
/// </summary>
internal record ShakeHatRequest
{
    /// <summary>
    /// The participants, with whatever they drew previously ignored — the shaker clears every pick
    /// before it starts, so a re-shake is not biased by the shake before it.
    /// </summary>
    public required ImmutableList<Participant> Participants { get; init; }

    /// <summary>One of <see cref="DrawTypes.All"/>.</summary>
    public required string DrawType { get; init; }

    /// <summary>
    /// How many independently seeded attempts to make. The shaker is randomized and does not
    /// backtrack, so a run that paints itself into a corner is retried rather than unwound; more
    /// attempts is how a constrained draw gets its chances back.
    /// </summary>
    public required int Attempts { get; init; }
}
