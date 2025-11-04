namespace GiftExchange.Library.Models;

public record Participant
{
    public required Person PickedRecipient { get; init; }

    [Required]
    public required Person Person { get; init; }

    public required bool InvitationSent { get; init; }

    [Required, MinLength(1, ErrorMessage = "Each participant needs at least one possible recipient")]
    public required ImmutableList<Recipient> Recipients { get; init; }
}

public static class Participants
{
    public static Participant Empty => new()
    {
        Person = Persons.Empty,
        InvitationSent = false,
        PickedRecipient = Persons.Empty,
        Recipients = []
    };
}

