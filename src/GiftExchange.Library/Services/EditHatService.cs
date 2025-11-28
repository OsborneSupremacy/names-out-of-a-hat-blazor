namespace GiftExchange.Library.Services;

public class EditHatService : IBusinessService<EditHatRequest, StatusCodeOnlyResponse>
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    public EditHatService(GiftExchangeProvider giftExchangeProvider)
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(EditHatRequest request, ILambdaContext context)
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
