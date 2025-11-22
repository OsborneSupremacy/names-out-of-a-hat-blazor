namespace GiftExchange.Library.Handlers;

public class GetParticipant : ApiGatewayHandler<GetParticipantRequest, GetParticipantService, Participant>, IHasRequestParameters<GetParticipantRequest>, IHasResponseBody<Participant>
{
    public GetParticipant() { }

    public GetParticipant(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public Result<GetParticipantRequest> Transform(APIGatewayProxyRequest request)
    {
        var organizerEmail = request.PathParameters["organizerEmail"] ?? string.Empty;
        var hatId = Guid.TryParse(request.PathParameters["hatId"], out var hatid) ? hatid : Guid.Empty;
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
}
