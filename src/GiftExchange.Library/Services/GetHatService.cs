namespace GiftExchange.Library.Services;

public class GetHatService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly JsonService _jsonService;

    public GetHatService(
        GiftExchangeProvider giftExchangeProvider,
        JsonService jsonService
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var organizerEmail = request.QueryStringParameters["email"] ?? string.Empty;
        var hatId = Guid.TryParse(request.QueryStringParameters["id"], out var id) ? id : Guid.Empty;
        var showPickedRecipients = bool.TryParse(request.QueryStringParameters["showpickedrecipients"], out var boolOut) && boolOut;

        var getHatRequest = new GetHatRequest
        {
            OrganizerEmail = organizerEmail,
            HatId = hatId,
            ShowPickedRecipients = showPickedRecipients
        };

        var result = await GetHasAsync(getHatRequest, context);

        return result.IsFaulted ?
            ProxyResponseBuilder.Build(result.StatusCode, result.Exception.Message) :
            ProxyResponseBuilder.Build(result.StatusCode, _jsonService.SerializeDefault(result.Value));
    }

    public async Task<Result<Hat>> GetHasAsync(GetHatRequest request, ILambdaContext context)
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
