using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model;

public record Recipient
{
    public Recipient()
    {
    }

    public Recipient(Person person, bool eligible)
    {
        Person = person;
        Eligible = eligible;
    }

    [Required]
    public Person Person { get; set; } = default!;

    [Required]
    public bool Eligible { get; set; }
}

/// <summary>
/// TODO: Use this
/// </summary>
public class RecipientValidator : AbstractValidator<Recipient>
{
    public RecipientValidator()
    {
        RuleFor(x => x.Person).NotNull();
    }
}
