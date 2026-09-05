namespace GiftExchange.Library.Models;

public record Participant
{
    public required string PickedRecipient { get; init; }

    public required Person Person { get; init; }

    public required ImmutableList<string> EligibleRecipients { get; init; }

    /// <summary>
    /// How far the last email this exchange sent them is known to have got — one of
    /// <see cref="Models.DeliveryStatus"/>, and empty until something is heard.
    /// </summary>
    /// <remarks>
    /// Empty is the state before anything is sent and for a while after it: SES publishes events
    /// asynchronously, so a participant sits here for seconds even when everything works. It means
    /// "nothing heard", and never "not delivered". Anything showing this to an organizer has to say
    /// so, because the two readings lead to opposite actions — one is a reason to check an address,
    /// the other a reason to wait.
    /// </remarks>
    public required string DeliveryStatus { get; init; }

    /// <summary>
    /// Why, when <see cref="DeliveryStatus"/> is one of the ones with a why: the bounce subtype and
    /// what the receiving server said. Empty otherwise.
    /// </summary>
    /// <remarks>
    /// Written by a stranger's mail server rather than by this application or by an organizer, so
    /// it is the one field on this record that has passed through no moderation at all. Whatever
    /// renders it encodes it.
    /// </remarks>
    public required string DeliveryDetail { get; init; }

    /// <summary>
    /// Which of this application's emails <see cref="DeliveryStatus"/> is about — one of
    /// <see cref="EmailMessageType"/>, and empty while nothing has been heard.
    /// </summary>
    /// <remarks>
    /// An exchange sends more than one thing to the same person, and the status here is the newest
    /// row rather than the invitation's. Without this an organizer reading "delivered" next to
    /// somebody who says they never saw the invitation has no way to tell whether that word is
    /// about the invitation at all — it may be the announcement that the exchange had finished,
    /// sent weeks later to an address that swallowed the first message.
    /// </remarks>
    public required string DeliveryMessageType { get; init; }

    /// <summary>
    /// When SES says the event behind <see cref="DeliveryStatus"/> happened.
    /// <see cref="DateTimeOffset.MinValue"/> while nothing has been heard, which is how the rest of
    /// this API spells a timestamp it does not have.
    /// </summary>
    /// <remarks>
    /// This is the fact an organizer can hand to a participant who cannot find their invitation.
    /// "Delivered" alone gives them nothing to search for; a date and a time tells them which
    /// morning to look in, which is the difference between finding it in a junk folder and
    /// concluding it was never sent.
    /// </remarks>
    public required DateTimeOffset DeliveryOccurredAt { get; init; }
}

internal static class Participants
{
    public static Participant Empty => new()
    {
        Person = Persons.Empty,
        PickedRecipient = string.Empty,
        EligibleRecipients = [],
        DeliveryStatus = Models.DeliveryStatus.Unknown,
        DeliveryDetail = string.Empty,
        DeliveryMessageType = string.Empty,
        DeliveryOccurredAt = DateTimeOffset.MinValue
    };
}
