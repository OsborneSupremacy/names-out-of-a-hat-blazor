namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record AssignRecipientsRequest : IOrganizerScopedRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    /// <summary>
    /// What shape the organizer wants this draw to come out in — one of <see cref="DrawTypes.All"/>.
    /// </summary>
    /// <remarks>
    /// Stated on every shake rather than stored on the exchange, because it describes how a draw
    /// was made and not a standing rule about the exchange. Once names are out, the constraint is
    /// baked into the assignment and there is nothing left for a stored value to govern; a
    /// re-shake is a new draw and gets asked again.
    ///
    /// The organizer's exclusions are not part of this. They are held on the participants and are
    /// applied to every draw whatever this says — this can only add to them.
    /// </remarks>
    public required string DrawType { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
