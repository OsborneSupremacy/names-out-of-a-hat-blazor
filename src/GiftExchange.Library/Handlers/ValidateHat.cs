namespace GiftExchange.Library.Handlers;

public class ValidateHat : ApiGatewayHandler<ValidateHatRequest, ValidationService, ValidateHatResponse>
{
    public ValidateHat() { }

    public ValidateHat(IServiceProvider serviceProvider) : base(serviceProvider) { }
}
