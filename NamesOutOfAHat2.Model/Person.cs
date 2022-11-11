using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model;

public record Person
{
    [Required]
    public Guid Id { get; set; } = default!;

    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = default!;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string Email { get; set; } = default!;
}

public class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
