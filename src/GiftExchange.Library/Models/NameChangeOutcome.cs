namespace GiftExchange.Library.Models;

/// <summary>
/// What became of an attempt to change the name a participant is known by.
/// </summary>
/// <remarks>
/// A name belongs to the person rather than to one membership, so the only way this fails — short
/// of the participant no longer being there — is a collision with somebody else's name in an
/// exchange the renamed person is in. That is refused rather than resolved: the alternative is
/// silently leaving one exchange with two people called the same thing, and the domain records
/// still identify a pick by name.
/// </remarks>
public enum NameChangeOutcome
{
    /// <summary>The participant now goes by the new name, everywhere they appear.</summary>
    Changed,

    /// <summary>Nobody in this exchange is recorded at the address given.</summary>
    ParticipantNotFound,

    /// <summary>
    /// Somebody else already goes by the new name in an exchange this person takes part in.
    /// </summary>
    NameAlreadyInExchange
}
