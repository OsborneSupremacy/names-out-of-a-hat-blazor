namespace GiftExchange.Library.Models;

internal record Participant
{
    public required Person PickedRecipient { get; init; }

    [Required]
    public required Person Person { get; init; }


    [Required, MinLength(1, ErrorMessage = "Each participant needs at least one possible recipient")]
    public required ImmutableList<Recipient> Recipients { get; init; }
}

internal static class Participants
{
    public static Participant Empty => new()
    {
        Person = Persons.Empty,
        PickedRecipient = Persons.Empty,
        Recipients = []
    };
}

