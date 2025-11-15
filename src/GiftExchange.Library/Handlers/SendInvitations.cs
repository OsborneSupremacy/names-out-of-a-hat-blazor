namespace GiftExchange.Library.Handlers;

public class SendInvitations : HandlerBase<SendInvitationsRequest, EnqueueInvitationsService, StatusCodeOnlyResponse>, IHasRequestBody<SendInvitationsRequest>
{
}
