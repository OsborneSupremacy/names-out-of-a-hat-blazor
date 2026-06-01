namespace GiftExchange.Library.Validators;

public class EditHatRequestValidator : AbstractValidator<EditHatRequest>
{
    public EditHatRequestValidator()
    {
        RuleFor(x => x.HatId)
            .NotEmpty();

        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(3, 50)
            .Matches(@"^[\p{L}\p{N}\s\-'.,&()]+$")
            .WithMessage("'Name' must only contain letters, numbers, spaces, and common punctuation.");

        RuleFor(x => x.AdditionalInformation)
            .MaximumLength(2000)
            .Must(x => string.IsNullOrEmpty(x) || (!x.Contains('<') && !x.Contains('>') && !x.Contains('\0')))
            .WithMessage("'Additional Information' must not contain HTML or control characters.");

        RuleFor(x => x.PriceRange)
            .MaximumLength(50)
            .Matches(@"^[\p{L}\p{N}\s\-$€£¥.,/]+$")
            .When(x => !string.IsNullOrEmpty(x.PriceRange))
            .WithMessage("'Price Range' must only contain letters, numbers, spaces, currency symbols, and common punctuation.");
    }
}
