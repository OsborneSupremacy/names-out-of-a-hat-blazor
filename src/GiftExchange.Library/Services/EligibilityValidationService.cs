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

        foreach (var person in people)
            if(!participants.SelectMany(p => p.EligibleRecipients).Contains(person.Name))
                errors.Add($"{person.Name} is not an eligible recipient for any participant. Their name will not be picked.");

        if (errors.Any())
            return Task.FromResult(
                new Result<ValidateHatResponse>(new ValidateHatResponse { Success = false, Errors = errors.ToImmutableList() }, HttpStatusCode.OK)
            );

        return Task.FromResult(new Result<ValidateHatResponse>(
            new ValidateHatResponse { Success = true, Errors = [] }, HttpStatusCode.OK)
        );
    }
}
