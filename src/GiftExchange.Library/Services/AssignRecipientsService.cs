namespace GiftExchange.Library.Services;

public class AssignRecipientsService : IBusinessService<AssignRecipientsRequest, StatusCodeOnlyResponse>
{
    private readonly ILogger<AssignRecipientsService> _logger;

    private readonly ValidationService _validationService;

    private readonly DynamoDbService _dynamoDbService;

    private const int ShakeAttempts = 25;

    public AssignRecipientsService(
        ILogger<AssignRecipientsService> logger,
        DynamoDbService dynamoDbService,
        ValidationService validationService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(AssignRecipientsRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _dynamoDbService
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

        // await _dynamoDbService
        //     .UpdateParticipantsAsync(request.OrganizerEmail, request.HatId, participantsOut)
        //     .ConfigureAwait(false);

        await _dynamoDbService
            .UpdateRecipientsAssignedAsync(request.OrganizerEmail, request.HatId, true)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
