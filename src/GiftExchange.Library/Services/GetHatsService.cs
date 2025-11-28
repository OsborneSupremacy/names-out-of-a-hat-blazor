namespace GiftExchange.Library.Services;

public class GetHatsService : IBusinessService<GetHatsRequest, GetHatsResponse>
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    public GetHatsService(GiftExchangeProvider giftExchangeProvider)
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    public async Task<Result<GetHatsResponse>> ExecuteAsync(GetHatsRequest request, ILambdaContext context)
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
