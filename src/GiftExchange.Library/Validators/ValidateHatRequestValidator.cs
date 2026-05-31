namespace GiftExchange.Library.Validators;

public class ValidateHatRequestValidator : AbstractValidator<ValidateHatRequest>
{
    public ValidateHatRequestValidator()
    {
        RuleFor(x => x.HatId)
            .NotEmpty();

        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);
    }
}
