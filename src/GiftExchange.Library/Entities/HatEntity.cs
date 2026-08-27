namespace GiftExchange.Library.Entities;

/// <summary>
/// Persistence shape of a gift exchange. Mutable and reference-typed, unlike the immutable
/// <see cref="Models.Hat"/> record the rest of the application passes around.
/// </summary>
public class HatEntity
{
    public required Guid Id { get; set; }

    public required string OrganizerEmail { get; set; }

    public required string OrganizerName { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// Lower-cased, trimmed copy of <see cref="Name"/>. Written by the data layer, never by a
    /// caller; it exists so the unique index can compare names case-insensitively.
    /// </summary>
    public required string NameNormalized { get; set; }

    public required string Status { get; set; }

    public string AdditionalInformation { get; set; } = string.Empty;

    public string PriceRange { get; set; } = string.Empty;

    public DateTimeOffset? InvitationsQueuedAt { get; set; }

    /// <summary>
    /// The address invitations were sent from. Null until they are sent, and never supplied by a
    /// client — it comes from the request context, so it cannot be spoofed by the caller.
    /// </summary>
    public string? InvitationsSentFromIp { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public HatStatusEntity HatStatus { get; set; } = null!;

    public ICollection<ParticipantEntity> Participants { get; set; } = [];
}
