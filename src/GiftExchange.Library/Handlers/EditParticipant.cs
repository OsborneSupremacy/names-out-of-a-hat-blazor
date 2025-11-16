namespace GiftExchange.Library.Handlers;

public class EditParticipant : ApiGatewayHandler<EditParticipantRequest, EditParticipantService, StatusCodeOnlyResponse>, IHasRequestBody<EditParticipantRequest>
{
}
