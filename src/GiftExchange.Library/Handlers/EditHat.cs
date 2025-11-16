namespace GiftExchange.Library.Handlers;

public class EditHat : ApiGatewayHandler<EditHatRequest, EditHatService, StatusCodeOnlyResponse>, IHasRequestBody<EditHatRequest>
{
}
