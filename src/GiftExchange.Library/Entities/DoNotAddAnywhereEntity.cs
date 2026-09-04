namespace GiftExchange.Library.Entities;

/// <summary>
/// An address that must never be added to any gift exchange by anybody.
///
/// The widest of the three lists, and the closest thing this application has to an unsubscribe.
/// Delivery events have always been recorded, but a bounce or a complaint never stopped the next
/// send; this does.
/// </summary>
/// <remarks>
/// Nothing removes these rows. Somebody who changes their mind is added again by an organizer who
/// asks them first — there is deliberately no self-service way back in, because an address that can
/// un-block itself from a link is an address anybody who reaches that inbox can un-block.
/// </remarks>
public class DoNotAddAnywhereEntity
{
    public required Guid DoNotAddAnywhereId { get; set; }

    /// <summary>The refusing address, lower-cased and trimmed.</summary>
    public required string EmailNormalized { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
}
