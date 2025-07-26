namespace NamesOutOfAHat2.Model.Validators;

public class HatValidator : AbstractValidator<Hat>
{
    public HatValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AdditionalInformation).MaximumLength(1000);
        RuleFor(x => x.PriceRange).MaximumLength(255);

        RuleForEach(x => x.Participants)
            .SetValidator(new ParticipantValidator());

        RuleFor(x => x.Participants)
            .Must(x => x.Count >= 3)
            .WithMessage("A gift exchange like this needs at least three people.")
            .Must(x => x.Count <= 30)
            .WithMessage("30 is the maximum number of gift exchange participants.")
            .Must(x => IdsAreUnique(x))
            .WithMessage("Participants must have unique IDs.")
            .Must(x => NamesAreUnique(x))
            .WithMessage("Participants must have unique names. If multiple participants have the same name, please differentiate them in some way (middle/last initial, city, etc.).")
            .Must(x => EmailsAreUnique(x))
            .WithMessage("Participants must have unique email addresses.");
    }

    private delegate bool ParticipantDuplicateCheck(IList<Participant> items);

    private static readonly ParticipantDuplicateCheck NamesAreUnique = (items) =>
    {
        var uniqueCount = items
            .Select(x => x.Person.Name.Trim().ToLowerInvariant())
            .Distinct()
            .Count();
        return items.Count.Equals(uniqueCount);
    };

    private static readonly ParticipantDuplicateCheck EmailsAreUnique = (items) =>
    {
        var uniqueCount = items
            .Select(x => x.Person.Email.Trim().ToLowerInvariant())
            .Distinct()
            .Count();
        return items.Count.Equals(uniqueCount);
    };

    private static readonly ParticipantDuplicateCheck IdsAreUnique = (items) =>
    {
        var uniqueCount = items
            .Select(x => x.Person.Id)
            .Distinct()
            .Count();
        return items.Count.Equals(uniqueCount);
    };

    private static readonly Func<Participant, bool> HasEligibleRecipients = (participant) =>
    {
        if (!(participant.Recipients?.Any(x => x.Eligible) ?? false))
            return false;
        return false;
    };

    private static readonly Func<Person, List<Participant>, bool> IsARecipient = (person, participants) => participants
        .Where(x => x.Person.Id != person.Id)
        .SelectMany(x => x.Recipients)
        .Any(x => x.Eligible && x.Person.Id == person.Id);
}
