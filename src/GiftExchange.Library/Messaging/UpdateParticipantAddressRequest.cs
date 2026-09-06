namespace GiftExchange.Library.Messaging;

/// <summary>
/// What <c>GiftExchangeProvider.UpdateParticipantAddressAsync</c> needs to move one participant
/// onto a different email address.
/// </summary>
/// <remarks>
/// Distinct from <see cref="EditParticipantAddressRequest"/>, which is the API contract. That one
/// carries the organizer's address because the endpoint is scoped to them and the adapter overwrites
/// it with the authenticated caller; by the time the work reaches the provider, ownership has
/// already been established and the hat id is the whole scope.
/// </remarks>
internal record UpdateParticipantAddressRequest
{
    public required Guid HatId { get; init; }

    /// <summary>
    /// The organizer making the correction. Not scope — ownership is settled before the work
    /// reaches the provider — but authorship: an address the application has never seen becomes a
    /// new person, and this is who introduced them, which is what decides whose name it is to
    /// change afterwards.
    /// </summary>
    public required string OrganizerEmail { get; init; }

    /// <summary>The address as it stands, which is how the participant is found.</summary>
    public required string CurrentEmail { get; init; }

    public required string NewEmail { get; init; }
}
