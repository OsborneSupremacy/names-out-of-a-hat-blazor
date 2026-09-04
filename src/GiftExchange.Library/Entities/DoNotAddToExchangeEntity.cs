namespace GiftExchange.Library.Entities;

/// <summary>
/// An address that must never be added to one particular gift exchange again.
///
/// Written when somebody leaves that exchange from the link in their invitation, and the only one
/// of the three lists recorded without being asked for: leaving an exchange is itself the statement
/// that they do not want to be in it.
/// </summary>
/// <remarks>
/// Outlives both the participant row and the hat. That is deliberate — the block exists precisely
/// for the state where the participant row is gone, so deleting it alongside would leave nothing
/// behind at the only moment it matters.
/// </remarks>
public class DoNotAddToExchangeEntity
{
    public required Guid DoNotAddToExchangeId { get; set; }

    /// <summary>The exchange they left. No navigation property, in keeping with the rest of this schema.</summary>
    public required Guid HatId { get; set; }

    /// <summary>
    /// The refusing address, lower-cased and trimmed. Normalized on the way in rather than at the
    /// point of comparison, so that the index can answer the question rather than a scan and a
    /// function call.
    /// </summary>
    public required string EmailNormalized { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
}
