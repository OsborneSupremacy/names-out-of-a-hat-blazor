namespace NamesOutOfAHat2.Model.Validators;

public class RecipientValidator : AbstractValidator<Recipient>
{
    public RecipientValidator()
    {
        RuleFor(x => x.Person).NotNull();
    }
}
