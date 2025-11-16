namespace GiftExchange.Library.Handlers;

public class CreateHat : ApiGatewayHandler<CreateHatRequest, CreateHatService, CreateHatResponse>, IHasRequestBody<CreateHatRequest>, IHasRequestBody<CreateHatResponse>
{
}
