namespace GiftExchange.Library.Services;

internal class AssignRecipientsService : IApiGatewayHandler
{
    private readonly ILogger<AssignRecipientsService> _logger;

    private readonly ApiGatewayAdapter _adapter;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private const int ShakeAttempts = 25;

    private const string NonViableConfigurationMessage = "We've tried shaking the hat multiple times but we could not find a valid distribution (i.e. everyone is assigned to exactly one other participant). This can sometimes happen with certain configurations of participants and their eligible recipients. You can try shaking the hat again, but if the issue persists please review the list of participants and their eligible recipients to ensure that a valid distribution is possible.";

    public AssignRecipientsService(
        ILogger<AssignRecipientsService> logger,
        ApiGatewayAdapter adapter,
        GiftExchangeProvider giftExchangeProvider,
        HatPreconditionValidator hatPreconditionValidator
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
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
        {
            _logger.LogWarning("Hat Id {HatId} for organizer {OrganizerEmail} could not be shaken successfully after {ShakeAttempts} attempts. This likely indicates a non-viable configuration of participants and eligible recipients.", request.HatId, request.OrganizerEmail, ShakeAttempts);
            return new Result<StatusCodeOnlyResponse>(new OperationCanceledException(NonViableConfigurationMessage), HttpStatusCode.ServiceUnavailable);
        }

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
            .UpdateHatStatusAsync(request.OrganizerEmail, request.HatId, HatStatus.NamesAssigned)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
