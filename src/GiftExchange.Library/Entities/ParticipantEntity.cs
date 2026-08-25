namespace GiftExchange.Library.Entities;

public class ParticipantEntity
{
    public required Guid Id { get; set; }

    public required Guid HatId { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    /// <summary>
    /// The participant this one draws. Null until the hat is shaken. Referring to a participant
    /// by id rather than by display name is what allows two people to share a name.
    /// </summary>
    public Guid? PickedRecipientId { get; set; }

    public HatEntity Hat { get; set; } = null!;

    public ParticipantEntity? PickedRecipient { get; set; }

    /// <summary>Rows saying who this participant is allowed to draw.</summary>
    public ICollection<ParticipantEligibleRecipientEntity> EligibleRecipients { get; set; } = [];

    /// <summary>Rows saying which participants are allowed to draw this one.</summary>
    public ICollection<ParticipantEligibleRecipientEntity> EligibleFor { get; set; } = [];
}
