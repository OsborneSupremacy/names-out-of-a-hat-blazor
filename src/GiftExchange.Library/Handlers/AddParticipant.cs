namespace GiftExchange.Library.Handlers;

[UsedImplicitly]
public class AddParticipant : HandlerBase<AddParticipantRequest, AddParticipantService, StatusCodeOnlyResponse>, IHasRequestBody<AddParticipantRequest>
{
}
