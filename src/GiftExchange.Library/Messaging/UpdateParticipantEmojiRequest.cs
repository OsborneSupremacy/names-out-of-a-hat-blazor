namespace GiftExchange.Library.Messaging;

/// <summary>
/// What <c>GiftExchangeProvider.UpdateParticipantEmojiAsync</c> needs to change the face one
/// participant is marked with.
/// </summary>
/// <remarks>
/// Distinct from <see cref="EditParticipantEmojiRequest"/>, which is the API contract, for the
/// reason <see cref="UpdateParticipantAddressRequest"/> gives: by the time the work reaches the
/// provider the organizer's ownership of the hat has been established, and the hat id is the whole
/// scope.
/// </remarks>
internal record UpdateParticipantEmojiRequest
{
    public required Guid HatId { get; init; }

    /// <summary>The address the participant is recorded at, which is how they are found.</summary>
    public required string ParticipantEmail { get; init; }

    /// <summary>One of <c>PersonEmoji.All</c>. Checked before it gets this far.</summary>
    public required string Emoji { get; init; }
}
