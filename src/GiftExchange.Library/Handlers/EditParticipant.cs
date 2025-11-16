namespace GiftExchange.Library.Handlers;

public class EditParticipant : ApiGatewayHandler<EditParticipantRequest, EditParticipantService, StatusCodeOnlyResponse>, IHasRequestBody<EditParticipantRequest>
{
    public EditParticipant() { }

    public EditParticipant(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
