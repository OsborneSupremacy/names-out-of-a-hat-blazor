namespace GiftExchange.Library.Handlers;

public class SendInvitations : ApiGatewayHandler<SendInvitationsRequest, EnqueueInvitationsService, StatusCodeOnlyResponse>, IHasRequestBody<SendInvitationsRequest>
{
    public SendInvitations() { }

    public SendInvitations(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
