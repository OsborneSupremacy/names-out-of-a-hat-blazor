namespace GiftExchange.Library.Services;

internal class GetHatService : IApiGatewayHandler
{
    private readonly ApiGatewayAdapter _adapter;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public GetHatService(ApiGatewayAdapter adapter, GiftExchangeProvider giftExchangeProvider)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var organizerEmail = request.QueryStringParameters["email"] ?? string.Empty;
        var hatId = Guid.TryParse(request.QueryStringParameters["id"], out var id) ? id : Guid.Empty;
        var showPickedRecipients = bool.TryParse(request.QueryStringParameters["showpickedrecipients"], out var boolOut) && boolOut;

        return await _adapter
            .AdaptAsync(
                new GetHatRequest
                {
                    OrganizerEmail = organizerEmail,
                    HatId = hatId,
                    ShowPickedRecipients = showPickedRecipients
                },
                GetHasAsync)
            .ConfigureAwait(false);
    }

    public async Task<Result<Hat>> GetHasAsync(GetHatRequest request)
    {
        var (hatExists, hat) = await _giftExchangeProvider
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
