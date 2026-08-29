namespace GiftExchange.Library.Messaging;

/// <summary>
/// The answer to any request for a throttled slot: whether it was granted, and if not, when the one
/// blocking it was taken.
/// </summary>
/// <remarks>
/// One record for both the Ask and the address change, because both are asking the same question
/// and both need the same two facts back. What differs between them is who the slot belongs to,
/// which is what their separate request records are for.
///
/// The timestamp is here rather than left to the caller to invent, because a throttle that only
/// says no forces whoever is refused to guess at a date. Both callers put it in front of a person:
/// the Ask tells the asker when they last asked, and the address change tells the organizer when
/// they last corrected it.
/// </remarks>
internal record ReserveSlotResponse
{
    public required bool Reserved { get; init; }

    /// <summary>
    /// When the slot that blocked this one was taken. <see cref="DateTimeOffset.MinValue"/> whenever
    /// the slot was granted, and also when the previous timestamp could not be read back — callers
    /// word the message differently rather than showing a date they do not have.
    /// </summary>
    public required DateTimeOffset PreviouslyReservedAt { get; init; }
}

internal static class ReserveSlotResponses
{
    public static ReserveSlotResponse Reserved =>
        new() { Reserved = true, PreviouslyReservedAt = DateTimeOffset.MinValue };

    public static ReserveSlotResponse RefusedSince(DateTimeOffset previouslyReservedAt) =>
        new() { Reserved = false, PreviouslyReservedAt = previouslyReservedAt };

    /// <summary>Refused without a date. What a failure to reach DynamoDB looks like.</summary>
    public static ReserveSlotResponse Refused => RefusedSince(DateTimeOffset.MinValue);
}
