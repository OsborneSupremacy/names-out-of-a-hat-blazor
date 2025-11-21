namespace GiftExchange.Library.Models;

public record Participant
{
    public required string PickedRecipient { get; init; }

    public required Person Person { get; init; }

    public required ImmutableList<string> EligibleRecipients { get; init; }
}

internal static class Participants
{
    public static Participant Empty => new()
    {
        Person = Persons.Empty,
        PickedRecipient = string.Empty,
        EligibleRecipients = []
    };
}

