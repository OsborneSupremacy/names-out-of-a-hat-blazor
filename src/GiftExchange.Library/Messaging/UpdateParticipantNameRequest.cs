namespace GiftExchange.Library.Messaging;

/// <summary>
/// What <c>GiftExchangeProvider.UpdateParticipantNameAsync</c> needs to rename one participant.
/// </summary>
/// <remarks>
/// Distinct from <see cref="EditParticipantNameRequest"/>, which is the API contract. Ownership of
/// the hat has already been established by the time the work reaches the provider, so
/// <see cref="OrganizerEmail"/> is carried here for one narrow purpose rather than for scope: a
/// rename is felt in every exchange the person is in, and telling the organizer which one it
/// collided with is only safe for the exchanges they run themselves.
/// </remarks>
internal record UpdateParticipantNameRequest
{
    public required Guid HatId { get; init; }

    /// <summary>The address the participant is recorded at, which is how they are found.</summary>
    public required string ParticipantEmail { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// The caller, used only to decide which colliding exchanges may be named back to them.
    /// </summary>
    public required string OrganizerEmail { get; init; }
}
