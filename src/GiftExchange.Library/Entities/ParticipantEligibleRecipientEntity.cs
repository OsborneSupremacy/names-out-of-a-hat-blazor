namespace GiftExchange.Library.Entities;

/// <summary>
/// Join row between two participants in the same hat. Modelled as an explicit entity rather
/// than a skip navigation because the table carries its own surrogate key.
/// </summary>
public class ParticipantEligibleRecipientEntity
{
    public required Guid ParticipantEligibleRecipientsId { get; set; }

    public required Guid ParticipantId { get; set; }

    public required Guid EligibleParticipantId { get; set; }

    public ParticipantEntity Participant { get; set; } = null!;

    public ParticipantEntity EligibleParticipant { get; set; } = null!;
}
