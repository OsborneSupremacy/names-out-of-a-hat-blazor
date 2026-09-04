namespace GiftExchange.Library.Messaging;

/// <summary>
/// What the data layer needs in order to start a new gift exchange from a finished one.
/// </summary>
/// <remarks>
/// Distinct from <see cref="CopyHatRequest"/>, which is what a client sends. That one names the
/// copy and nothing else; by the time it reaches the provider the new hat has been described in
/// full and the refusals have been looked up, and neither of those is the caller's to supply over
/// the wire.
///
/// A record rather than four parameters mainly because of the last two. A bool and a set are easy
/// to read past, and both decide who is left out of the copy — one for a rule the organizer set,
/// the other for a refusal they are not allowed to overrule.
/// </remarks>
[UsedImplicitly]
internal record CopyHatDataRequest
{
    /// <summary>The finished exchange being copied. Also the scope the refusals were recorded in.</summary>
    public required Guid SourceHatId { get; init; }

    /// <summary>The copy, already named and described.</summary>
    public required HatDataModel NewHat { get; init; }

    /// <summary>
    /// Leave everybody out of their own previous recipient's eligibility list, so that nobody draws
    /// the person they drew last time.
    /// </summary>
    public required bool ExcludePreviousRecipients { get; init; }

    /// <summary>
    /// Addresses that must not be carried over, normalized. The organizer is copied regardless of
    /// what is in here — they are a participant of their own exchange, and a list they joined for
    /// somebody else's is not a reason to remove them from it.
    /// </summary>
    public required ImmutableHashSet<string> RefusedEmails { get; init; }
}
