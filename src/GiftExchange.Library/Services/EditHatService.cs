namespace GiftExchange.Library.Services;

public class EditHatService : IBusinessService<EditHatRequest, StatusCodeOnlyResponse>
{
    private readonly GiftExchangeDataProvider _giftExchangeDataProvider;

    public EditHatService(GiftExchangeDataProvider giftExchangeDataProvider)
    {
        _giftExchangeDataProvider = giftExchangeDataProvider ?? throw new ArgumentNullException(nameof(giftExchangeDataProvider));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(EditHatRequest request, ILambdaContext context)
    {
        var (hatExists, _ ) = await _giftExchangeDataProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(
                new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound
            );

        await _giftExchangeDataProvider.EditHatAsync(request)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
