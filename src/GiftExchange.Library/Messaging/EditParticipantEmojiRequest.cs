namespace GiftExchange.Library.Messaging;

/// <summary>
/// A change to the face one participant is marked with.
///
/// Separate from <see cref="EditParticipantRequest"/>, which edits eligibility and resets the hat
/// to IN_PROGRESS when it does — the same reason <see cref="EditParticipantAddressRequest"/> is its
/// own thing. A face is decoration, and changing one should not throw away a draw.
/// </summary>
[UsedImplicitly]
public record EditParticipantEmojiRequest : IOrganizerScopedRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    /// <summary>The address the participant is recorded at, which is how they are found.</summary>
    public required string Email { get; init; }

    /// <summary>
    /// The face to mark them with. One of a closed list the application offers, which is what makes
    /// this safe to store and render without moderation.
    /// </summary>
    public required string Emoji { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
