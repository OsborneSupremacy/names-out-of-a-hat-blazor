namespace GiftExchange.Library.Handlers;

public class CreateHat : ApiGatewayHandler<CreateHatRequest, CreateHatService, CreateHatResponse>, IHasRequestBody<CreateHatRequest>, IHasRequestBody<CreateHatResponse>
{
    public CreateHat() { }

    public CreateHat(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
