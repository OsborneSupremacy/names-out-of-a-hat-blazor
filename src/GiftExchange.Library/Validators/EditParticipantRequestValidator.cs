namespace GiftExchange.Library.Validators;

public class EditParticipantRequestValidator : AbstractValidator<EditParticipantRequest>
{
    public EditParticipantRequestValidator()
    {
        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.HatId)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.EligibleRecipients)
            .NotEmpty()
            .Must(x => x.Count > 0)
            .WithMessage("At least one eligible recipient is required");
    }
}
