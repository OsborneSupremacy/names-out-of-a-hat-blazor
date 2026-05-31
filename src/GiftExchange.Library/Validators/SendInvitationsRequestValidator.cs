namespace GiftExchange.Library.Validators;

public class SendInvitationsRequestValidator : AbstractValidator<SendInvitationsRequest>
{
    public SendInvitationsRequestValidator()
    {
        RuleFor(x => x.HatId)
            .NotEmpty();

        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);
    }
}
