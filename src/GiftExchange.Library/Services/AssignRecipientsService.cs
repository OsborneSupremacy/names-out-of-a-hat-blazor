namespace GiftExchange.Library.Services;

internal class AssignRecipientsService : IApiGatewayHandler
{
    private readonly ILogger<AssignRecipientsService> _logger;

    private readonly ApiGatewayAdapter _adapter;

    private readonly ValidationService _validationService;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private const int ShakeAttempts = 25;

    public AssignRecipientsService(
        ILogger<AssignRecipientsService> logger,
        ApiGatewayAdapter adapter,
        GiftExchangeProvider giftExchangeProvider,
        ValidationService validationService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync<AssignRecipientsRequest, StatusCodeOnlyResponse>(request, AssignRecipientsAsync);

    public async Task<Result<StatusCodeOnlyResponse>> AssignRecipientsAsync(
        AssignRecipientsRequest request
        )
    {
        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        // the validation method should have been called first, but we'll re-validate.
        // Will not return details since the client should get those from the validation endpoint.
        var validResult = await _validationService
            .ValidateAsync(new ValidateHatRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail
            })
            .ConfigureAwait(false);

        if (validResult.IsFaulted || validResult.Value.Success)
            return new Result<StatusCodeOnlyResponse>(new AggregateException("Validation failed"), HttpStatusCode.BadRequest);

        var (shakeSuccess, participantsOut) = HatShakerService
            .ShakeMultiple(hat.Participants, ShakeAttempts);

        if (!shakeSuccess)
            return new Result<StatusCodeOnlyResponse>(new OperationCanceledException($"Valid recipient distribution not found after {ShakeAttempts} attempts"), HttpStatusCode.ServiceUnavailable);

        var updateParticipantsTasks = new List<Task>();

        foreach (var participant in participantsOut)
            updateParticipantsTasks.Add(_giftExchangeProvider
                .UpdateParticipantPickedRecipientAsync(
                    request.OrganizerEmail,
                    request.HatId,
                    participant.Person.Email,
                    participant.PickedRecipient
                ));

        await Task.WhenAll(updateParticipantsTasks)
            .ConfigureAwait(false);

        await _giftExchangeProvider
            .UpdateRecipientsAssignedAsync(request.OrganizerEmail, request.HatId, true)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
