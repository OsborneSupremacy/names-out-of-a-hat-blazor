using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model;

public record Participant
{
    public Participant()
    {
    }

    public Participant(Person person)
    {
        Person = person;
    }

    public Person? PickedRecipient { get; set; }

    [Required]
    public Person Person { get; set; } = default!;

    [Required, MinLength(1, ErrorMessage = "Each participant needs at least one possible recipient")]
    public IList<Recipient> Recipients { get; set; } = default!;
}

/// <summary>
/// TODO: Use this
/// </summary>
public class ParticipantValidator : AbstractValidator<Participant>
{
    public ParticipantValidator()
    {
        RuleFor(x => x.Person).NotNull();
        RuleFor(x => x.Recipients)
            .NotNull();

        When(x => x.Recipients is not null, () => {
            RuleFor(x => x.Recipients)
                .Must(x => x.Count >= 1)
                .WithMessage("Each participant needs at least one possible recipient.");
        });
    }
}
