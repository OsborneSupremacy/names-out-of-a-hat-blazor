namespace GiftExchange.Library.Validators;

internal class EditParticipantAddressRequestValidator : AbstractValidator<EditParticipantAddressRequest>
{
    public EditParticipantAddressRequestValidator()
    {
        RuleFor(x => x.OrganizerEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.HatId)
            .NotEmpty();

        // Both addresses face the rules a participant address faces anywhere else. The current one
        // is a lookup key rather than something being stored, but an address that could never have
        // been stored will not find anybody, and saying so here is clearer than a 404.
        RuleFor(x => x.CurrentEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .EmailAddress()
            .Length(5, 254);

        // Changing an address to itself would resend the invitation while changing nothing, which
        // is a resend button wearing a disguise. If resending to the same address is ever wanted,
        // it should be its own deliberate thing rather than a side effect of a no-op edit.
        RuleFor(x => x.NewEmail)
            .NotEqual(x => x.CurrentEmail)
            .WithMessage("'New Email' must be different from the current address.");
    }
}
