namespace GiftExchange.Library.Entities;

/// <summary>
/// A person taking part in one gift exchange.
///
/// Carries no name or address of its own — those belong to the <see cref="PersonEntity"/> it points
/// at. What it adds is everything true only within this hat: who they drew, and, through
/// <see cref="EligibleRecipients"/>, who they were allowed to draw.
/// </summary>
public class ParticipantEntity
{
    public required Guid ParticipantId { get; set; }

    public required Guid HatId { get; set; }

    public required Guid PersonId { get; set; }

    /// <summary>
    /// The participant this one draws, or the all-zero <see cref="Guid"/> until the hat is shaken.
    /// Referring to a participant by id rather than by display name is what allows two people to
    /// share a name.
    /// </summary>
    /// <remarks>
    /// Deliberately has no navigation property. EF turns a reference navigation into a real foreign
    /// key constraint wherever the provider supports one, and the all-zero sentinel would violate
    /// it the moment a participant was written without a pick. Picks are resolved in the provider
    /// instead, against the participants of the same hat.
    /// </remarks>
    public required Guid PickedRecipientParticipantId { get; set; }

    /// <summary>
    /// The face this participant is marked with wherever they are named — one of
    /// <c>PersonEmoji.All</c>, assigned when they are added and the organizer's to change.
    /// </summary>
    /// <remarks>
    /// On the participant rather than the person, so it is one of the things true only within this
    /// hat. That is the difference between an organizer marking somebody in their own exchange and
    /// an organizer reaching into every other exchange that person is in — which is what a name
    /// edit does, deliberately, and what a face edit should not.
    /// </remarks>
    public required string Emoji { get; set; }

    public HatEntity Hat { get; set; } = null!;

    public PersonEntity Person { get; set; } = null!;

    /// <summary>Rows saying who this participant is allowed to draw.</summary>
    public ICollection<ParticipantEligibleRecipientEntity> EligibleRecipients { get; set; } = [];

    /// <summary>Rows saying which participants are allowed to draw this one.</summary>
    public ICollection<ParticipantEligibleRecipientEntity> EligibleFor { get; set; } = [];
}
