namespace GiftExchange.Library.Services;

[UsedImplicitly]
public class EditParticipantService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly JsonService _jsonService;

    public EditParticipantService(
        GiftExchangeProvider giftExchangeProvider,
        JsonService jsonService
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var innerRequest = request.GetInnerRequest<EditParticipantRequest>(_jsonService);

        if(innerRequest.IsFaulted)
            return ProxyResponseBuilder.Build(innerRequest.StatusCode, innerRequest.Exception.Message);

        var result = await EditParticipantAsync(innerRequest.Value, context);

        return result.IsFaulted ?
            ProxyResponseBuilder.Build(result.StatusCode, result.Exception.Message) :
            ProxyResponseBuilder.Build(result.StatusCode);
    }

    public async Task<Result<StatusCodeOnlyResponse>> EditParticipantAsync(
        EditParticipantRequest request,
        ILambdaContext context
        )
    {
        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        var (participantExists, participant) = await _giftExchangeProvider
            .GetParticipantAsync(
                request.OrganizerEmail,
                request.HatId,
                request.Email
            )
            .ConfigureAwait(false);

        if(!participantExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Participant with email `{request.Email}` not found"), HttpStatusCode.NotFound);

        if(request.EligibleRecipients.Contains(participant.Person.Name, StringComparer.OrdinalIgnoreCase))
            return new Result<StatusCodeOnlyResponse>(new ArgumentException("Participant cannot set themselves as an eligible recipient"), HttpStatusCode.BadRequest);

        var otherParticipants = hat.Participants
            .Where(p => !p.Person.Name.ContentEquals(participant.Person.Name))
            .Select(p => p.Person.Name)
            .ToImmutableList();

        var invalidRecipients = request.EligibleRecipients
            .Where(r => !otherParticipants.Contains(r, StringComparer.OrdinalIgnoreCase))
            .ToImmutableList();

        if (invalidRecipients.Any())
        {
            var errorMessage = $"""
                                One or more provided recipients are not part of this gift exchange.

                                Gift exchange participants: {string.Join(", ", otherParticipants)}
                                Provided Recipients: {string.Join(", ", request.EligibleRecipients)}
                                Invalid Recipients: {string.Join(", ", invalidRecipients)}


                                """;

            return new Result<StatusCodeOnlyResponse>(new ArgumentException(errorMessage), HttpStatusCode.BadRequest);
        }

        await _giftExchangeProvider
            .UpdateEligibleRecipientsAsync(
                request.OrganizerEmail,
                request.HatId,
                request.Email,
                request.EligibleRecipients
            )
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
