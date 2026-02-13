namespace GiftExchange.Library.Services;

internal class ValidationService : IApiGatewayHandler
{
    private const int Max = 30;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly ApiGatewayAdapter _adapter;

    public ValidationService(
        GiftExchangeProvider giftExchangeProvider,
        HatPreconditionValidator hatPreconditionValidator,
        ApiGatewayAdapter adapter)
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context) =>
        _adapter.AdaptAsync<ValidateHatRequest, ValidateHatResponse>(request, ValidateAsync);

    public async Task<Result<ValidateHatResponse>> ValidateAsync(ValidateHatRequest request)
    {
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = [],
                ValidHatStatuses = [ HatStatus.InProgress, HatStatus.ReadyForAssignment ]
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<ValidateHatResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        var hat = hatPreconditionResult.Hat;

        var result = await ValidateAsync(hat);

        var newStatus = (result.IsFaulted || !result.Value.Success) ? HatStatus.InProgress : HatStatus.ReadyForAssignment;

        await _giftExchangeProvider
            .UpdateHatStatusAsync(request.OrganizerEmail, request.HatId, newStatus);

        return result;
    }

    internal async Task<Result<ValidateHatResponse>> ValidateAsync(Hat hat)
    {
        var validCountResponse = ValidateCount(hat.Participants);
        if(validCountResponse.IsFaulted || !validCountResponse.Value.Success)
            return validCountResponse;

        var validEligibilityResponse = await EligibilityValidationService.Validate(hat.Participants);
        if(validEligibilityResponse.IsFaulted || !validEligibilityResponse.Value.Success)
            return validEligibilityResponse;

        return new Result<ValidateHatResponse>(new ValidateHatResponse { Success = true, Errors = []}, HttpStatusCode.OK);
    }

    private static Result<ValidateHatResponse> ValidateCount(IList<Participant> participants)
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
