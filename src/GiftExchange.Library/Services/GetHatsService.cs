namespace GiftExchange.Library.Services;

internal class GetHatsService : IApiGatewayHandler
{
    private readonly ApiGatewayAdapter _adapter;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public GetHatsService(
        ApiGatewayAdapter adapter,
        GiftExchangeProvider giftExchangeProvider
        )
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var organizerEmail = request.PathParameters.TryGetValue("email", out var email)
            ? System.Web.HttpUtility.UrlDecode(email)
            : string.Empty;
        return _adapter.AdaptAsync(new GetHatsRequest
        {
            OrganizerEmail = organizerEmail
        }, ExecuteAsync);
    }

    internal async Task<Result<GetHatsResponse>> ExecuteAsync(GetHatsRequest request)
    {
        var result = await _giftExchangeProvider
            .GetHatsAsync(request.OrganizerEmail)
            .ConfigureAwait(false);

        return new Result<GetHatsResponse>(new GetHatsResponse
        {
            Hats = result
        }, HttpStatusCode.OK);
    }
}
