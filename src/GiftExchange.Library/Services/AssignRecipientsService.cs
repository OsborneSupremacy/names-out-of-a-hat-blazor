namespace GiftExchange.Library.Services;

internal class AssignRecipientsService : IApiGatewayHandler
{
    private readonly ILogger<AssignRecipientsService> _logger;

    private readonly JsonService _jsonService;

    private readonly ValidationService _validationService;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private const int ShakeAttempts = 25;

    public AssignRecipientsService(
        ILogger<AssignRecipientsService> logger,
        GiftExchangeProvider giftExchangeProvider,
        ValidationService validationService,
        JsonService jsonService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var innerRequest = request.GetInnerRequest<AssignRecipientsRequest>(_jsonService);

        if(innerRequest.IsFaulted)
            return ProxyResponseBuilder.Build(innerRequest.StatusCode, innerRequest.Exception.Message);

        var result = await AssignRecipientsAsync(innerRequest.Value, context);

        return result.IsFaulted ?
            ProxyResponseBuilder.Build(result.StatusCode, result.Exception.Message) :
            ProxyResponseBuilder.Build(result.StatusCode);
    }

    public async Task<Result<StatusCodeOnlyResponse>> AssignRecipientsAsync(
        AssignRecipientsRequest request,
        ILambdaContext context
        )
    {
        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        // the validation method should have been called first, but we'll re-validate.
        // Will not return details since the client should get those from the validation endpoint.
        var validResult = await _validationService.ExecuteAsync(new ValidateHatRequest
        {
            HatId = request.HatId,
            OrganizerEmail = request.OrganizerEmail
        }, context);

        if (validResult.IsFaulted || validResult.Value.Success)
            return new Result<StatusCodeOnlyResponse>(new AggregateException("Validation failed"), HttpStatusCode.BadRequest);

        var (shakeSuccess, participantsOut) = HatShakerService.ShakeMultiple(hat.Participants, ShakeAttempts);

        if (!shakeSuccess)
            return new Result<StatusCodeOnlyResponse>(new OperationCanceledException($"Valid recipient distribution not found after {ShakeAttempts} attempts"), HttpStatusCode.ServiceUnavailable);

        // await _giftExchangeProvider
        //     .UpdateParticipantsAsync(request.OrganizerEmail, request.HatId, participantsOut)
        //     .ConfigureAwait(false);

        await _giftExchangeProvider
            .UpdateRecipientsAssignedAsync(request.OrganizerEmail, request.HatId, true)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
