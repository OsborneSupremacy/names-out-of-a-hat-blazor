namespace GiftExchange.Library.Validators;

internal class EditParticipantEmojiRequestValidator : AbstractValidator<EditParticipantEmojiRequest>
{
    public EditParticipantEmojiRequestValidator()
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

        // Membership of the offered list, not a length or a character class. It is the only check
        // this field needs and the only one that would hold: an emoji is several bytes and any
        // number of code points, so a rule written in terms of size would either admit text or
        // refuse a face. Refused here rather than in the service, so nothing arbitrary reaches a
        // column the interface renders unescaped.
        RuleFor(x => x.Emoji)
            .NotEmpty()
            .Must(PersonEmoji.IsOffered)
            .WithMessage("'Emoji' must be one of the faces this application offers.");
    }
}
