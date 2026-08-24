namespace GiftExchange.Library.Validators;

public class RedeemMagicLinkRequestValidator : AbstractValidator<RedeemMagicLinkRequest>
{
    public RedeemMagicLinkRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .Length(16, 128);
    }
}
