namespace GiftExchange.Library.Messaging;

/// <summary>
/// What became of an attempt to rename one participant.
/// </summary>
internal record UpdateParticipantNameResponse
{
    public required NameChangeOutcome Outcome { get; init; }

    /// <summary>The participant that was renamed, or the all-zero id when nobody was.</summary>
    public required Guid ParticipantId { get; init; }

    /// <summary>
    /// The name they went by before, or the empty string when nothing was written. Worth returning
    /// so the log line says what actually changed rather than only that something did.
    /// </summary>
    public required string PreviousName { get; init; }

    /// <summary>
    /// Exchanges run by the caller where the new name is already taken. Empty unless
    /// <see cref="Outcome"/> is <see cref="NameChangeOutcome.NameAlreadyInExchange"/>, and empty
    /// even then when the only collisions are in exchanges somebody else runs.
    /// </summary>
    public required ImmutableList<string> ConflictingHatNames { get; init; }

    /// <summary>
    /// Whether the new name is taken in an exchange the caller does not run.
    /// </summary>
    /// <remarks>
    /// A flag rather than a list, deliberately. The organizer needs to know that a rename they
    /// cannot see the reason for is being refused, and needs to be able to tell that from a
    /// collision in one of their own exchanges — but naming another organizer's exchange, or
    /// counting them, would tell them things about somebody else's guest list that they have no
    /// standing to learn.
    /// </remarks>
    public required bool ConflictsElsewhere { get; init; }
}

internal static class UpdateParticipantNameResponses
{
    /// <summary>Nobody was renamed. The shape every failure starts from.</summary>
    public static UpdateParticipantNameResponse For(NameChangeOutcome outcome) =>
        new()
        {
            Outcome = outcome,
            ParticipantId = Guid.Empty,
            PreviousName = string.Empty,
            ConflictingHatNames = [],
            ConflictsElsewhere = false
        };
}
