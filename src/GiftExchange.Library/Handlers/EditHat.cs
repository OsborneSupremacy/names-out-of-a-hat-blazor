namespace GiftExchange.Library.Handlers;

public class EditHat : ApiGatewayHandler<EditHatRequest, EditHatService, StatusCodeOnlyResponse>, IHasRequestBody<EditHatRequest>
{
    [UsedImplicitly]
    public EditHat() { }

    public EditHat(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
