namespace GiftExchange.Library.Validators;

internal class CloseHatRequestValidator : AbstractValidator<CloseHatRequest>
{
    public CloseHatRequestValidator()
    {
        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.HatId)
            .NotEmpty();
    }
}
