namespace GiftExchange.Library.Services;

internal class RemoveParticipantService : IApiGatewayHandler
{
    ILogger<RemoveParticipantService> _logger;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly ApiGatewayAdapter _adapter;

    public RemoveParticipantService(
        ILogger<RemoveParticipantService> logger,
        GiftExchangeProvider giftExchangeProvider,
        ApiGatewayAdapter adapter
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context) =>
        _adapter.AdaptAsync<RemoveParticipantRequest, StatusCodeOnlyResponse>(request, ExecuteAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(RemoveParticipantRequest request)
    {
        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        if(request.Email.ContentEquals(hat.Organizer.Email))
            return new Result<StatusCodeOnlyResponse>(new InvalidOperationException("The organizer cannot be removed."), HttpStatusCode.BadRequest);

        var participant = hat.Participants
            .FirstOrDefault(p => p.Person.Email.ContentEquals(request.Email));

        if(participant is null)
            return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.NoContent }, HttpStatusCode.NoContent);

        // remove participant from eligible recipients of all other participants
        await _giftExchangeProvider
            .RemoveParticipantFromEligibleRecipientsAsync(
                request.OrganizerEmail,
                request.HatId,
                participant.Person.Name
            )
            .ConfigureAwait(false);

        // remove participant from hat
        await _giftExchangeProvider
            .DeleteParticipantAsync(request.OrganizerEmail, request.HatId, request.Email)
            .ConfigureAwait(false);

        // set recipients assigned to false since the hat composition has changed
        if(hat.RecipientsAssigned)
            await _giftExchangeProvider.UpdateRecipientsAssignedAsync(request.OrganizerEmail, request.HatId, false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.NoContent}, HttpStatusCode.NoContent);
    }

}
