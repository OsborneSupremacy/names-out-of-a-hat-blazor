namespace GiftExchange.Library.Services;

public class ValidationService : IBusinessService<ValidateHatRequest, ValidateHatResponse>
{
    private const int Max = 30;

    private readonly DynamoDbService _dynamoDbService;

    public ValidationService(DynamoDbService dynamoDbService)
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result<ValidateHatResponse>> ExecuteAsync(ValidateHatRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<ValidateHatResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        var validCountResponse = ValidateCount(hat.Participants);
            if(validCountResponse.IsFaulted || !validCountResponse.Value.Success) return validCountResponse;

        var validEligibilityResponse = new EligibilityValidationService().Validate(hat.Participants);
            if(validEligibilityResponse.IsFaulted || !validEligibilityResponse.Value.Success) return validEligibilityResponse;

        return new Result<ValidateHatResponse>(new ValidateHatResponse { Success = true, Errors = []}, HttpStatusCode.OK);
    }

    private Result<ValidateHatResponse> ValidateCount(IList<Participant> participants)
    {
        var (isValid, error) = participants.Count switch
        {
            < 3 => (false, "A gift exchange of this type needs at least three people."),
            > Max => (false, $"{Max} people is the maximum number of participants supported."),
            _ => (true, string.Empty)
        };

        return isValid switch
        {
            false => new Result<ValidateHatResponse>(new ValidateHatResponse { Success = false, Errors = [error] },
                HttpStatusCode.OK),
            _ => new Result<ValidateHatResponse>(new ValidateHatResponse { Success = true, Errors = [] },
                HttpStatusCode.OK)
        };
    }
}
