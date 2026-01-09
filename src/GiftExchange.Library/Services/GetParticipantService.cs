namespace GiftExchange.Library.Services;

internal class GetParticipantService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly ApiGatewayAdapter _apiGatewayAdapter;

    public GetParticipantService(
        GiftExchangeProvider giftExchangeProvider,
        ApiGatewayAdapter apiGatewayAdapter
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _apiGatewayAdapter = apiGatewayAdapter ?? throw new ArgumentNullException(nameof(apiGatewayAdapter));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var innerRequest = GetInnerRequest(request);
        return _apiGatewayAdapter
            .AdaptAsync(innerRequest.Value, GetParticipantAsync);
    }

    private Result<GetParticipantRequest> GetInnerRequest(APIGatewayProxyRequest request)
    {
        var organizerEmail = request.PathParameters["organizerEmail"] ?? string.Empty;
        var hatId = Guid.TryParse(request.PathParameters["hatId"], out var id) ? id : Guid.Empty;
        var participantEmail = request.PathParameters["participantEmail"] ?? string.Empty;
        var showPickedRecipients = bool.TryParse(request.QueryStringParameters["showpickedrecipients"], out var boolOut) && boolOut;

        return new Result<GetParticipantRequest>(new GetParticipantRequest
        {
            OrganizerEmail = organizerEmail,
            HatId = hatId,
            ParticipantEmail = participantEmail,
            ShowPickedRecipients = showPickedRecipients
        }, HttpStatusCode.OK);
    }

    internal async Task<Result<Participant>> GetParticipantAsync(GetParticipantRequest request)
    {
        var (participantExists, participant) = await _giftExchangeProvider
            .GetParticipantAsync(request.OrganizerEmail, request.HatId, request.ParticipantEmail)
            .ConfigureAwait(false);

        if (!participantExists)
            return new Result<Participant>(
                new KeyNotFoundException($"Participant with email {request.ParticipantEmail} not found"),
                HttpStatusCode.NotFound);

        if(!request.ShowPickedRecipients)
            participant = RedactPickedRecipient(participant);

        return new Result<Participant>(participant, HttpStatusCode.OK);
    }

    private Participant RedactPickedRecipient(Participant participant) =>
        participant with
        {
            PickedRecipient = string.IsNullOrWhiteSpace(participant.PickedRecipient)
                ? string.Empty
                : Persons.Redacted.Name
        };

}
