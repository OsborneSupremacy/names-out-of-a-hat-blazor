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

    /// <summary>The address as it stands, which is how the participant is found.</summary>
    public required string CurrentEmail { get; init; }

    public required string NewEmail { get; init; }
}
