namespace GiftExchange.Library.Validators;

public class RequestMagicLinkRequestValidator : AbstractValidator<RequestMagicLinkRequest>
{
    public RequestMagicLinkRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);
    }
}
