namespace GiftExchange.Library.Services;

public class AssignRecipientsService : IServiceWithoutResponseBody<AssignRecipientsRequest>
{
    private readonly ILogger<AssignRecipientsService> _logger;

    private readonly DynamoDbService _dynamoDbService;

    private const int ShakeAttempts = 25;

    public AssignRecipientsService(ILogger<AssignRecipientsService> logger, DynamoDbService dynamoDbService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result> ExecuteAsync(AssignRecipientsRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        // the validation method should have been called first, but we'll re-validate.
        // Will not return details since the client should get those from the validation endpoint.
        var validResult = new ValidationService().Validate(hat);
        if (!validResult.Success) return new Result(HttpStatusCode.BadRequest);

        var (shakeSuccess, participantsOut) = HatShakerService.ShakeMultiple(hat.Participants, ShakeAttempts);

        if (!shakeSuccess)
            return new Result(new OperationCanceledException($"Valid recipient distribution not found after {ShakeAttempts} attempts"), HttpStatusCode.ServiceUnavailable);

        await _dynamoDbService
            .UpdateParticipantsAsync(request.OrganizerEmail, request.HatId, participantsOut)
            .ConfigureAwait(false);

        await _dynamoDbService
            .UpdateRecipientsAssignedAsync(request.OrganizerEmail, request.HatId, true)
            .ConfigureAwait(false);

        return new Result(HttpStatusCode.OK);
    }
}
