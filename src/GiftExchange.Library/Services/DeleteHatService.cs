namespace GiftExchange.Library.Services;

internal class DeleteHatService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly ApiGatewayAdapter _adapter;

    public DeleteHatService(
        GiftExchangeProvider giftExchangeProvider,
        ApiGatewayAdapter adapter
    )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        =>
            _adapter.AdaptAsync<DeleteHatRequest, StatusCodeOnlyResponse>(request, DeleteHatAsync);

    public async Task<Result<StatusCodeOnlyResponse>> DeleteHatAsync(DeleteHatRequest request)
    {
        await _giftExchangeProvider
            .DeleteHatAsync(request)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.NoContent}, HttpStatusCode.NoContent);
    }
}
