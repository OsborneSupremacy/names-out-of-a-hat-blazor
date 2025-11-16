namespace GiftExchange.Library.Handlers;

public class RemoveParticipant : ApiGatewayHandler<RemoveParticipantRequest, RemoveParticipantService, StatusCodeOnlyResponse>, IHasRequestBody<RemoveParticipantRequest>
{
    public RemoveParticipant() { }

    public RemoveParticipant(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
