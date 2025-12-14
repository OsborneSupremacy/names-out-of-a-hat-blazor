namespace GiftExchange.Library.Services;

public class EditHatService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly JsonService _jsonService;

    public EditHatService(
        GiftExchangeProvider giftExchangeProvider,
        JsonService jsonService
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var innerRequest = request.GetInnerRequest<EditHatRequest>(_jsonService);

        if(innerRequest.IsFaulted)
            return ProxyResponseBuilder.Build(innerRequest.StatusCode, innerRequest.Exception.Message);

        var result = await EditHatAsync(innerRequest.Value, context);

        return result.IsFaulted ?
            ProxyResponseBuilder.Build(result.StatusCode, result.Exception.Message) :
            ProxyResponseBuilder.Build(result.StatusCode, _jsonService.SerializeDefault(result.Value));
    }

    public async Task<Result<StatusCodeOnlyResponse>> EditHatAsync(EditHatRequest request, ILambdaContext context)
    {
        var (hatExists, _ ) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(
                new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound
            );

        await _giftExchangeProvider.EditHatAsync(request)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
