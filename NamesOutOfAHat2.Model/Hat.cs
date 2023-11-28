using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model;

public class Hat
{
    public Hat()
    {
        Id = Guid.NewGuid();
        Participants = new List<Participant>();
        Errors = new List<string>();
    }

    [Required]
    public Guid Id { get; set; }

    public List<string> Errors { get; set; }

    public string? Name { get; set; }

    [MaxLength(10000)]
    public string? AdditionalInformation { get; set; } = default!;

    [MaxLength(255)]
    public string? PriceRange { get; set; } = default!;

    public Participant? Organizer { get; set; }

    [Required,
        MinLength(3, ErrorMessage = "A gift exchange like this needs at least three people"),
        MaxLength(30, ErrorMessage = "30 is the maximum number of gift exchange participants.")
    ]
    public IList<Participant> Participants { get; set; }
}

/// <summary>
/// TODO: Use this
/// </summary>
public class HatValidator : AbstractValidator<Hat>
{
    public HatValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        When(x => x.AdditionalInformation is not null, () => {
            RuleFor(x => x.AdditionalInformation)
                .MaximumLength(1000)
                .When(x => x.AdditionalInformation is not null);
        });

        When(x => x.PriceRange is not null, () => {
            RuleFor(x => x.PriceRange)
                .MaximumLength(255)
                .When(x => x.PriceRange is not null);
        });

        RuleForEach(x => x.Participants)
            .SetValidator(new ParticipantValidator());

        RuleFor(x => x.Participants)
            .Must(x => x.Count >= 3)
            .WithMessage("A gift exchange like this needs at least three people.")
            .Must(x => x.Count <= 30)
            .WithMessage("30 is the maximum number of gift exchange participants.")
            .Must(x => _idsAreUnique(x))
            .WithMessage("Participants must have unique IDs.")
            .Must(x => _namesAreUnique(x))
            .WithMessage("Participants must have unique names. If multiple participants have the same name, please differentiate them in some way (middle/last initial, city, etc.).")
            .Must(x => _emailsAreUnique(x))
            .WithMessage("Participants must have unique email addresses.")

            ;
    }

    private delegate bool ParticipantDuplicateCheck(IList<Participant> items);

    private static readonly ParticipantDuplicateCheck _namesAreUnique = (IList<Participant> items) =>
    {
        var uniqueCount = items
            .Select(x => x.Person.Name.Trim().ToLowerInvariant())
            .Distinct()
            .Count();
        return items.Count.Equals(uniqueCount);
    };

    private static readonly ParticipantDuplicateCheck _emailsAreUnique = (IList<Participant> items) =>
    {
        var uniqueCount = items
            .Select(x => x.Person.Email.Trim().ToLowerInvariant())
            .Distinct()
            .Count();
        return items.Count.Equals(uniqueCount);
    };

    private static readonly ParticipantDuplicateCheck _idsAreUnique = (IList<Participant> items) =>
    {
        var uniqueCount = items
            .Select(x => x.Person.Id)
            .Distinct()
            .Count();
        return items.Count.Equals(uniqueCount);
    };

    private static readonly Func<Participant, bool> _hasEligibleRecipients = (Participant participant) =>
    {
        if (!(participant.Recipients?.Any(x => x.Eligible) ?? false))
            return false;
        return false;
    };

    private static readonly Func<Person, List<Participant>, bool> _isARecipient = (Person person, List<Participant> participants) =>
    {
        return participants
            .Where(x => x.Person.Id != person.Id)
            .SelectMany(x => x.Recipients)
            .Any(x => x.Eligible && x.Person.Id == person.Id);
    };
}
