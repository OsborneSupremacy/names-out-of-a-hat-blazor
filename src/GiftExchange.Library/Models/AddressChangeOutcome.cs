namespace GiftExchange.Library.Models;

/// <summary>
/// What became of an attempt to correct the address one participant was invited at.
///
/// The three failures are all conflicts with the rest of the exchange rather than anything wrong
/// with the address itself, which is why each names what it collided with: the organizer needs to
/// know whether to pick a different address or to go and fix something else first.
/// </summary>
public enum AddressChangeOutcome
{
    /// <summary>The participant now points at the new address.</summary>
    Changed,

    /// <summary>Nobody in this exchange is recorded at the address given.</summary>
    ParticipantNotFound,

    /// <summary>
    /// Somebody else in this exchange already has the new address. Two participants cannot share
    /// one, which <c>uq_participant_hat_person</c> enforces underneath.
    /// </summary>
    AddressAlreadyInExchange,

    /// <summary>
    /// The new address belongs to a person whose name is already taken by another participant here.
    /// </summary>
    /// <remarks>
    /// A name is global to a person, and the domain records still identify participants within a
    /// hat by name, so moving somebody onto an address that belongs to an existing person can
    /// rename them into a collision. Refused rather than resolved, because the alternative is
    /// renaming a real person across every exchange they are in to make one of them fit.
    /// </remarks>
    NameAlreadyInExchange
}
