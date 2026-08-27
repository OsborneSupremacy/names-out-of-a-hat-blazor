namespace GiftExchange.Library.Services;

/// <summary>
/// Starts a new gift exchange from a finished one: the same people, the same eligibility rules,
/// and — by default — nobody drawing the person they drew last time.
/// </summary>
internal class CopyHatService : IApiGatewayHandler
{
    /// <summary>Worded exactly as it is for a hat created from scratch, so the advice matches.</summary>
    private const string NameTakenMessage =
        "A gift exchange with this name already exists. If this is the same gift exchange for a different year, try adding the year in the name to differentiate it from previous exchanges.";

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly ApiGatewayAdapter _adapter;

    public CopyHatService(
        GiftExchangeProvider giftExchangeProvider,
        HatPreconditionValidator hatPreconditionValidator,
        ApiGatewayAdapter adapter
    )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync<CopyHatRequest, CopyHatResponse>(request, CopyHatAsync);

    internal async Task<Result<CopyHatResponse>> CopyHatAsync(CopyHatRequest request)
    {
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = new Dictionary<string, string>
                {
                    ["gift exchange name"] = request.NewHatName
                },
                // Only a revealed exchange can be copied. Before that the picks are still secret,
                // and leaving out last year's recipient would quietly disclose them.
                ValidHatStatuses = [HatStatus.Closed]
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<CopyHatResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        var (nameTaken, _) = await _giftExchangeProvider
            .DoesHatAlreadyExistAsync(request.OrganizerEmail, request.NewHatName)
            .ConfigureAwait(false);

        if (nameTaken)
            return new Result<CopyHatResponse>(
                new InvalidOperationException(NameTakenMessage),
                HttpStatusCode.Conflict);

        var sourceHat = hatPreconditionResult.Hat;

        var newHat = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            HatName = request.NewHatName,
            Status = HatStatus.InProgress,
            AdditionalInformation = sourceHat.AdditionalInformation,
            PriceRange = sourceHat.PriceRange,
            OrganizerEmail = request.OrganizerEmail,
            OrganizerName = sourceHat.Organizer.Name
        };

        var copied = await _giftExchangeProvider
            .CopyHatAsync(request.HatId, newHat, request.ExcludePreviousRecipients)
            .ConfigureAwait(false);

        if (!copied)
            return new Result<CopyHatResponse>(
                new InvalidOperationException(NameTakenMessage),
                HttpStatusCode.Conflict);

        return new Result<CopyHatResponse>(new CopyHatResponse { HatId = newHat.HatId }, HttpStatusCode.Created);
    }
}
