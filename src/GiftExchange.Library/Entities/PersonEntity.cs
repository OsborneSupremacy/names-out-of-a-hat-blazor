namespace GiftExchange.Library.Entities;

/// <summary>
/// Somebody the application knows about, identified by their email address.
///
/// Organizers and participants are the same kind of thing, and both are rows in this table. A hat
/// points at the person who organizes it; a participant row points at the person taking part. Once
/// that is true, <see cref="Name"/> is stored in exactly one place, and renaming somebody is a
/// single write rather than a sweep over every hat they appear in.
///
/// One row per email address for the whole system, which is what the unique index on
/// <see cref="Email"/> enforces.
/// </summary>
public class PersonEntity
{
    public required Guid PersonId { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    /// <summary>Hats this person organizes.</summary>
    public ICollection<HatEntity> OrganizedHats { get; set; } = [];

    /// <summary>Gift exchanges this person takes part in.</summary>
    public ICollection<ParticipantEntity> Participations { get; set; } = [];
}
