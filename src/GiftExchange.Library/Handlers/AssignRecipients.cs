namespace GiftExchange.Library.Handlers;

[UsedImplicitly]
public class AssignRecipients
{
    private readonly DynamoDbService _dynamoDbService;

    private const int ShakeAttempts = 25;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public AssignRecipients()
    {
        _dynamoDbService = new DynamoDbService();
    }

    [UsedImplicitly]
    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler.FunctionHandler<AssignRecipientsRequest>
        (
            request,
            InnerHandler,
            context
        );

    private async Task<Result> InnerHandler(AssignRecipientsRequest request)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        // validation method should have been called first, but we'll re-validate.
        // Will not return details since the client should get those form the validate endpoint.
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
