namespace GiftExchange.Library.Handlers;

[UsedImplicitly]
public class AddParticipant : ApiGatewayHandler<AddParticipantRequest, AddParticipantService, StatusCodeOnlyResponse>, IHasRequestBody<AddParticipantRequest>
{
    public AddParticipant() { }

    public AddParticipant(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
