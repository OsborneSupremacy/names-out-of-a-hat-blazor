namespace GiftExchange.Library.Entities;

/// <summary>
/// Persistence shape of a gift exchange. Mutable and reference-typed, unlike the immutable
/// <see cref="Models.Hat"/> record the rest of the application passes around.
///
/// No property here is nullable, because no column is. Absence is a value: the all-zero
/// <see cref="Guid"/> for an id, <see cref="DateTimeOffset.MinValue"/> for a date, and the empty
/// string for text. Nor does any property carry an initialiser — the table has no defaults either,
/// so every field is stated by whoever writes the row.
/// </summary>
public class HatEntity
{
    public required Guid HatId { get; set; }

    /// <summary>The person who organizes this exchange. They are a person like any other.</summary>
    public required Guid OrganizerPersonId { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// Lower-cased, trimmed copy of <see cref="Name"/>. Written by the data layer, never by a
    /// caller; it exists so the unique index can compare names case-insensitively.
    /// </summary>
    public required string NameNormalized { get; set; }

    /// <summary>
    /// One of <see cref="Models.HatStatuses.All"/>. Held as a string rather than a foreign key to a
    /// reference table: DSQL has no foreign keys, so such a table could never have constrained
    /// this, and the application is what checks the value.
    /// </summary>
    public required string Status { get; set; }

    public required string AdditionalInformation { get; set; }

    public required string PriceRange { get; set; }

    /// <summary><see cref="DateTimeOffset.MinValue"/> until invitations are queued.</summary>
    public required DateTimeOffset InvitationsQueuedAt { get; set; }

    /// <summary>
    /// The address invitations were sent from. Empty until they are sent, and never supplied by a
    /// client — it comes from the request context, so it cannot be spoofed by the caller.
    /// </summary>
    public required string InvitationsSentFromIp { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The exchange this one was copied from, or the all-zero <see cref="Guid"/> if it was not
    /// copied. That id is the sentinel hat, so "not a copy" still names a row.
    /// </summary>
    /// <remarks>
    /// A self reference with no navigation, for the same reason
    /// <see cref="ParticipantEntity.PickedRecipientParticipantId"/> has none: a navigation would
    /// make EF emit a foreign key wherever the provider supports one, and DSQL supports none. The
    /// two would then disagree about whether the source hat can be deleted — the test databases
    /// would refuse it and DSQL would allow it. The provider clears this column instead, the same
    /// way it clears a pick.
    /// </remarks>
    public required Guid CopiedFromHatId { get; set; }

    public PersonEntity Organizer { get; set; } = null!;

    public ICollection<ParticipantEntity> Participants { get; set; } = [];
}
