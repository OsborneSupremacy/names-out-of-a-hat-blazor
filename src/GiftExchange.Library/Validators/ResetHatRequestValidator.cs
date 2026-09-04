namespace GiftExchange.Library.Validators;

internal class ResetHatRequestValidator : AbstractValidator<ResetHatRequest>
{
    public ResetHatRequestValidator()
    {
        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.HatId)
            .NotEmpty();
    }
}
