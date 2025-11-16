namespace GiftExchange.Library.Handlers;

[UsedImplicitly]
public class AssignRecipients : ApiGatewayHandler<AssignRecipientsRequest, AssignRecipientsService, StatusCodeOnlyResponse>, IHasRequestBody<AssignRecipientsRequest>
{
    public AssignRecipients() { }

    public AssignRecipients(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
