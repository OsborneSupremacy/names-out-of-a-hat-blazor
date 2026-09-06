namespace GiftExchange.Library.Models;

/// <summary>
/// What became of an attempt to change the name somebody goes by.
/// </summary>
/// <remarks>
/// A name is stored on the person and read back into every exchange they appear in, so both
/// failures here are about that reach rather than about the name itself: one says the rename was
/// not the caller's to make, and the other that somebody it would have reached already answers to
/// it. The two need different remedies, which is why they are different outcomes.
/// </remarks>
public enum NameChangeOutcome
{
    /// <summary>They now go by the new name, everywhere they appear.</summary>
    Changed,

    /// <summary>The application has never heard of the address given.</summary>
    PersonNotFound,

    /// <summary>
    /// Somebody else already goes by the new name in an exchange this person takes part in.
    /// </summary>
    NameAlreadyInExchange,

    /// <summary>
    /// The caller neither is this person nor introduced them, so the name is not theirs to change.
    /// </summary>
    /// <remarks>
    /// The refusal a shared participant needs. Two organizers can have the same person in their
    /// exchanges, and without this the second could rename them in the first's — repeatedly, and
    /// invisibly to everybody but the person themselves.
    /// </remarks>
    NotTheirNameToChange
}
