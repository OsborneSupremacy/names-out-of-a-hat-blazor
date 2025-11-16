namespace GiftExchange.Library.Handlers;

public class EditHat : ApiGatewayHandler<EditHatRequest, EditHatService, StatusCodeOnlyResponse>, IHasRequestBody<EditHatRequest>
{
    public EditHat() { }

    public EditHat(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
