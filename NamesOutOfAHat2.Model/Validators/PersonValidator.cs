namespace NamesOutOfAHat2.Model.Validators;

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
