namespace GiftExchange.Library.Services;

public class GetHatsService : IBusinessService<GetHatsRequest, GetHatsResponse>
{
    private readonly GiftExchangeDataProvider _giftExchangeDataProvider;

    public GetHatsService(GiftExchangeDataProvider giftExchangeDataProvider)
    {
        _giftExchangeDataProvider = giftExchangeDataProvider ?? throw new ArgumentNullException(nameof(giftExchangeDataProvider));
    }

    public async Task<Result<GetHatsResponse>> ExecuteAsync(GetHatsRequest request, ILambdaContext context)
    {
        var result = await _giftExchangeDataProvider
            .GetHatsAsync(request.OrganizerEmail)
            .ConfigureAwait(false);

        return new Result<GetHatsResponse>(new GetHatsResponse
        {
            Hats = result
        }, HttpStatusCode.OK);
    }
}
