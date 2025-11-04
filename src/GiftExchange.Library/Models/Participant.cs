using System.ComponentModel.DataAnnotations;

namespace GiftExchange.Library.Models;

public record Participant
{
    public required Person PickedRecipient { get; init; }

    [Required]
    public required Person Person { get; init; }

    [Required, MinLength(1, ErrorMessage = "Each participant needs at least one possible recipient")]
    public required IReadOnlyList<Recipient> Recipients { get; init; }
}

public static class Participants
{
    public static Participant Empty => new()
    {
        Person = Persons.Empty,
        PickedRecipient = Persons.Empty,
        Recipients = []
    };
}

