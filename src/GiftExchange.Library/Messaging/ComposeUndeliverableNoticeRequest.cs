namespace GiftExchange.Library.Messaging;

/// <summary>
/// The exchange, and the participants within it whose invitation is known not to have arrived.
/// </summary>
/// <remarks>
/// A record rather than two positional arguments because the second one is a subset of something
/// reachable through the first: <c>hat.Participants</c> is everybody, and this is the few. Passed
/// as a list of the same <see cref="Participant"/> records so that the notice quotes exactly what
/// the organizer's own delivery column shows -- the status, the reason the far end gave, and when
/// it happened -- rather than a second, differently-derived account of the same rows.
/// </remarks>
[UsedImplicitly]
internal record ComposeUndeliverableNoticeRequest
{
    public required Hat Hat { get; init; }

    /// <summary>
    /// Never empty. The service composing this does not call it when nothing failed, because an
    /// email saying nothing went wrong is one more thing an organizer has to open to find out it
    /// did not need opening.
    /// </summary>
    public required ImmutableList<Participant> Undeliverable { get; init; }
}
