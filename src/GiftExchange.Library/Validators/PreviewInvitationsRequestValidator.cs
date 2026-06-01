namespace GiftExchange.Library.Validators;

using FluentValidation;
using Messaging;

public class PreviewInvitationsRequestValidator : AbstractValidator<PreviewInvitationsRequest>
{
    public PreviewInvitationsRequestValidator()
    {
        RuleFor(x => x.HatId)
            .NotEmpty();

        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);
    }
}
