namespace GiftExchange.Library.Messaging;

/// <summary>
/// A correction to the address one participant was invited at.
///
/// Separate from <see cref="EditParticipantRequest"/>, which edits eligibility and resets the hat
/// to IN_PROGRESS when it does. That is right for a change made before the draw and catastrophic
/// after it: an address fixed once invitations had gone out would un-send the whole exchange.
/// </summary>
[UsedImplicitly]
public record EditParticipantAddressRequest : IOrganizerScopedRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    /// <summary>The address as it stands, which is how the participant is found.</summary>
    public required string CurrentEmail { get; init; }

    public required string NewEmail { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
