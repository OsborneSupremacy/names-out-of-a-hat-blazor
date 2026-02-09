namespace GiftExchange.Library.Services;

internal class EditHatService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly IContentModerationService _contentModerationService;

    private readonly ApiGatewayAdapter _adapter;

    public EditHatService(
        GiftExchangeProvider giftExchangeProvider,
        IContentModerationService contentModerationService,
        ApiGatewayAdapter adapter
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _contentModerationService = contentModerationService ?? throw new ArgumentNullException(nameof(contentModerationService));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync<EditHatRequest, StatusCodeOnlyResponse>(request, EditHatAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> EditHatAsync(EditHatRequest request)
    {
        // Validate content before processing
        var (isValid, errorMessages) = await _contentModerationService.ValidateMultipleFieldsAsync(
            new Dictionary<string, string>
            {
                ["gift exchange name"] = request.Name,
                ["additional information"] = request.AdditionalInformation,
                ["price range"] = request.PriceRange
            });

        if (!isValid)
        {
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(string.Join(" ", errorMessages)),
                HttpStatusCode.BadRequest
            );
        }

        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(
                new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound
            );

        if(hat.InvitationsQueued)
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException("Cannot edit hat after invitations have been sent."),
                HttpStatusCode.Conflict
            );

        await _giftExchangeProvider.EditHatAsync(request)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
