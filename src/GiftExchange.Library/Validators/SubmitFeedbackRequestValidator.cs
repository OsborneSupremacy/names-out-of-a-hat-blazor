namespace GiftExchange.Library.Validators;

public class SubmitFeedbackRequestValidator : AbstractValidator<SubmitFeedbackRequest>
{
    public SubmitFeedbackRequestValidator()
    {
        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(FeedbackCategories.All.Contains)
            .WithMessage($"'Category' must be one of: {string.Join(", ", FeedbackCategories.All)}.");

        // No character class rule, unlike the other free-text fields here. Those name people and
        // gift exchanges, and end up rendered into invitation HTML; this ends up as plain text in
        // one mailbox. Somebody reporting a bug should be able to paste the URL or the error they
        // saw without being told their punctuation is not allowed.
        RuleFor(x => x.Message)
            .NotEmpty()
            .Length(1, 4000);
    }
}
