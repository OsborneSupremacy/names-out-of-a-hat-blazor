namespace GiftExchange.Library.Services;

internal static class EligibilityValidationService
{
    public static Task<Result<ValidateHatResponse>> Validate(IList<Participant> participants)
    {
        var errors = new List<string>();

        errors.AddRange(
            participants
                .Where(p => p.EligibleRecipients.Count < 2)
                .Select(p => $"{p.Person.Name} must have at least two eligible recipients.")
        );

        if (errors.Any())
            return Task.FromResult(
                new Result<ValidateHatResponse>(new ValidateHatResponse { Success = false, Errors = errors.ToImmutableList() }, HttpStatusCode.OK)
            );

        var people = participants.Select(x => x.Person).ToList();

        var recipientEligibilityCounts = participants
            .SelectMany(p => p.EligibleRecipients)
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var person in people)
            if(!recipientEligibilityCounts.TryGetValue(person.Name, out var eligibleForCount) || eligibleForCount == 0)
                errors.Add($"{person.Name} is not an eligible recipient for any participant. Their name will not be picked.");
            else if (eligibleForCount == 1)
                errors.Add($"{person.Name} is only an eligible recipient for one participant. This makes their assignment deterministic.");

        if (errors.Any())
            return Task.FromResult(
                new Result<ValidateHatResponse>(new ValidateHatResponse { Success = false, Errors = errors.ToImmutableList() }, HttpStatusCode.OK)
            );

        return Task.FromResult(new Result<ValidateHatResponse>(
            new ValidateHatResponse { Success = true, Errors = [] }, HttpStatusCode.OK)
        );
    }
}
