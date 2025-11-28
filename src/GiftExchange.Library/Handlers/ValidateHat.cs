namespace GiftExchange.Library.Handlers;

public class ValidateHat : ApiGatewayHandler<ValidateHatRequest, ValidationService, ValidateHatResponse>, IHasRequestBody<ValidateHatRequest>, IHasResponseBody<ValidateHatResponse>
{
    public ValidateHat() { }

    public ValidateHat(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
