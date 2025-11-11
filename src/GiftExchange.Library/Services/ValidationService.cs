namespace GiftExchange.Library.Services;

internal class ValidationService
{
    private const int Max = 30;

    public ValidateHatResponse Validate(Hat hat)
    {
        var validCountResponse = ValidateCount(hat.Participants);
            if(!validCountResponse.Success) return validCountResponse;

        var validDuplicatesResponse = ValidateDuplicates(hat.Participants);
            if(!validDuplicatesResponse.Success) return validCountResponse;

        var validEligibilityResponse = new EligibilityValidationService().Validate(hat.Participants);
            if(!validEligibilityResponse.Success) return validCountResponse;

        return new ValidateHatResponse { Success = true, Errors = []};
    }

    private ValidateHatResponse ValidateCount(IList<Participant> participants)
    {
        var (isValid, error) = participants.Count switch
        {
            < 3 => (false, "A gift exchange of this type needs at least three people."),
            > Max => (false, $"{Max} people is the maximum number of participants supported."),
            _ => (true, string.Empty)
        };

        if (!isValid)
            return new ValidateHatResponse { Success = false, Errors = [ error ] };

        return new ValidateHatResponse { Success = true, Errors = []};
    }

    private ValidateHatResponse ValidateDuplicates(IList<Participant> participants)
    {
        List<DuplicateCheckService> duplicateCheckServices = [
            new NameDuplicateCheckService(),
            new EmailDuplicateCheckService()
        ];

        var people = participants.Select(x => x.Person).ToList();

        foreach (var duplicateCheckService in duplicateCheckServices)
        {
            var duplicatesCheckResult = duplicateCheckService.Execute(people);
            if (!duplicatesCheckResult.Success)
                return duplicatesCheckResult;
        }

        return new ValidateHatResponse { Success = true, Errors = []};
    }
}
