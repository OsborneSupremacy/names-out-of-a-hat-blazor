namespace GiftExchange.Library.Messaging;

/// <summary>
/// An organizer's request to correct one participant's address, put to the throttle.
/// </summary>
/// <remarks>
/// Per participant rather than per organizer, because the two failure modes pull apart the same way
/// the Ask's do. An organizer who sent to five typo'd addresses should be able to fix all five in
/// one sitting, which a per-organizer limit would prevent; what nobody needs is to re-point the same
/// participant at address after address, which is the shape abuse takes — one participant slot used
/// as a way to mail arbitrary strangers from a domain people trust.
///
/// This is friction rather than a security boundary, and it is worth being honest about which. What
/// actually bounds the feature is that the caller is a signed-in organizer acting on their own
/// exchange, every send is attributable to them, and the content has been through moderation. The
/// window only stops the same participant being used as a loop.
/// </remarks>
internal record ReserveAddressChangeSlotRequest
{
    public required Guid ParticipantId { get; init; }

    public required TimeSpan Window { get; init; }
}
