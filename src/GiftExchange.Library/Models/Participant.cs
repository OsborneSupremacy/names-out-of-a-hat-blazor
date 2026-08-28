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
}

internal static class Participants
{
    public static Participant Empty => new()
    {
        Person = Persons.Empty,
        PickedRecipient = string.Empty,
        EligibleRecipients = [],
        DeliveryStatus = Models.DeliveryStatus.Unknown,
        DeliveryDetail = string.Empty
    };
}
