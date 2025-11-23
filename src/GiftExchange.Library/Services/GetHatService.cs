namespace GiftExchange.Library.Services;

public class GetHatService : IBusinessService<GetHatRequest, Hat>
{
    private readonly GiftExchangeDataProvider _giftExchangeDataProvider;

    public GetHatService(GiftExchangeDataProvider giftExchangeDataProvider)
    {
        _giftExchangeDataProvider = giftExchangeDataProvider ?? throw new ArgumentNullException(nameof(giftExchangeDataProvider));
    }

    public async Task<Result<Hat>> ExecuteAsync(GetHatRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _giftExchangeDataProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<Hat>(new KeyNotFoundException($"HatId {hat.Id} not found"), HttpStatusCode.NotFound);

        if(!request.ShowPickedRecipients)
            hat = RedactPickedRecipients(hat);

        return new Result<Hat>(hat, HttpStatusCode.OK);
    }

    private Hat RedactPickedRecipients(Hat hat) =>
        hat with
        {
            Participants = hat.Participants
                .Select(p => p with
                {
                    PickedRecipient = string.IsNullOrWhiteSpace(p.PickedRecipient) ? string.Empty : Persons.Redacted.Name
                })
                .ToImmutableList()
        };
}
