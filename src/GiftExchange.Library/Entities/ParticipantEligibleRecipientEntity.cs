namespace GiftExchange.Library.Entities;

/// <summary>
/// Join row between two participants in the same hat. Modelled as an explicit entity rather than a
/// skip navigation because the table carries its own surrogate key.
///
/// Both ends are participant ids rather than person ids: eligibility is a fact about one exchange,
/// and the same two people may be eligible for each other in one and not in another.
/// </summary>
public class ParticipantEligibleRecipientEntity
{
    public required Guid ParticipantEligibleRecipientId { get; set; }

    public required Guid ParticipantId { get; set; }

    public required Guid EligibleParticipantId { get; set; }

    public ParticipantEntity Participant { get; set; } = null!;

    public ParticipantEntity EligibleParticipant { get; set; } = null!;
}
