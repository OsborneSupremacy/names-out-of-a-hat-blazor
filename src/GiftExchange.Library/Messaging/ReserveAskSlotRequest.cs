namespace GiftExchange.Library.Messaging;

/// <summary>
/// One participant's request to ask another for gift ideas, put to the throttle.
/// </summary>
/// <remarks>
/// Both ids, because the slot is held per pair rather than per asker. Nobody should be nagged,
/// which is about how often one person hears from us; and somebody who asked and got nothing back
/// should still be able to try somebody else, which is about how often one person may ask. A
/// per-asker limit serves the first and defeats the second — one silent recipient would lock the
/// asker out of the whole exchange for a week. A pair caps what any individual receives at one
/// request per asker per window, which is the number that actually reaches an inbox.
/// </remarks>
internal record ReserveAskSlotRequest
{
    public required Guid AskerParticipantId { get; init; }

    /// <summary>
    /// Who is being asked. Either the asker's own pick or another participant they hope has ideas.
    /// </summary>
    public required Guid TargetParticipantId { get; init; }

    public required TimeSpan Window { get; init; }
}
