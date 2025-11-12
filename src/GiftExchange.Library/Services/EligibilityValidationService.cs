namespace GiftExchange.Library.Services;

internal class EligibilityValidationService
{
    public ValidateHatResponse Validate(ImmutableList<Participant> participants)
    {
        var errors = new List<string>();

        foreach (var participant in participants)
        {
            if (!(participant.Recipients.Any()))
            {
                errors.Add(new($"{participant.Person.Name} has no possible recipients"));
                continue;
            }
            if (!(participant.Recipients.Any(x => x.Eligible)))
            {
                errors.Add(new($"{participant.Person.Name} has no eligible recipients"));
            }
        }

        if (errors.Any())
            return new ValidateHatResponse { Success = false, Errors = errors.ToImmutableList() };

        var people = participants.Select(x => x.Person).ToList();

        foreach (var person in people)
        {
            if (!participants
                .Where(x => x.Person.Email != person.Email)
                .SelectMany(x => x.Recipients)
                .Any(x => x.Eligible && x.Person.Email == person.Email))

                errors.Add(new($"{person.Name} is not an eligible recipient for any participant. Their name will not be picked."));
        }

        if (errors.Any())
            return new ValidateHatResponse { Success = false, Errors = errors.ToImmutableList() };

        return new ValidateHatResponse { Success = true, Errors =[] };
    }
}
