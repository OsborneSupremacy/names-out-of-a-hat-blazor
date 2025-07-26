using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model.DomainModels;

public record Participant
{
    public required Person PickedRecipient { get; init; }

    [Required]
    public required Person Person { get; init; }

    [Required, MinLength(1, ErrorMessage = "Each participant needs at least one possible recipient")]
    public required List<Recipient> Recipients { get; init; }

    public static implicit operator ParticipantViewModel(Participant p) => new ParticipantViewModel
    {
        Person = p.Person,
        PickedRecipient = p.PickedRecipient,
        Recipients = p.Recipients.Select(r => (RecipientViewModel)r).ToList()
    };
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

