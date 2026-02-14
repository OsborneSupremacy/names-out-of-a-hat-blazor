namespace GiftExchange.Library.Services;

internal class AssignRecipientsService : IApiGatewayHandler
{
    private readonly ILogger<AssignRecipientsService> _logger;

    private readonly ApiGatewayAdapter _adapter;

    private readonly ValidationService _validationService;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private const int ShakeAttempts = 25;

    public AssignRecipientsService(
        ILogger<AssignRecipientsService> logger,
        ApiGatewayAdapter adapter,
        GiftExchangeProvider giftExchangeProvider,
        HatPreconditionValidator hatPreconditionValidator,
        ValidationService validationService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync<AssignRecipientsRequest, StatusCodeOnlyResponse>(request, AssignRecipientsAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> AssignRecipientsAsync(
        AssignRecipientsRequest request
        )
    {
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = [],
                ValidHatStatuses = [
                    HatStatus.ReadyForAssignment,
                    HatStatus.NamesAssigned
                ]
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<StatusCodeOnlyResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        var hat = hatPreconditionResult.Hat;

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
