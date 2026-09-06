namespace GiftExchange.Library.Messaging;

/// <summary>
/// A change to the name one participant is known by.
///
/// Separate from <see cref="EditParticipantRequest"/>, which edits eligibility and resets the hat
/// to IN_PROGRESS when it does — the same reason <see cref="EditParticipantAddressRequest"/> and
/// <see cref="EditParticipantEmojiRequest"/> are their own things. Who somebody may draw is
/// unaffected by what they are called: eligibility and picks are held as participant ids, so a
/// rename changes nothing about the draw and must not throw one away.
/// </summary>
[UsedImplicitly]
public record EditParticipantNameRequest : IOrganizerScopedRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    /// <summary>The address the participant is recorded at, which is how they are found.</summary>
    public required string Email { get; init; }

    /// <summary>
    /// The name to call them by. Moderated before it is written, because unlike an emoji it is free
    /// text an organizer typed, and it is read by everybody else in the exchange.
    /// </summary>
    public required string Name { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
