namespace GiftExchange.Library.Handlers;

public class ValidateHat
{
    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public ValidateHat()
    {
        _dynamoDbService = new DynamoDbService();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler.FunctionHandler<ValidateHatRequest, ValidateHatResponse>
        (
            request,
            InnerHandler,
            context
        );
    private async Task<Result<ValidateHatResponse>> InnerHandler(ValidateHatRequest request)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<ValidateHatResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        var validResult = new ValidationService().Validate(hat);

        return new Result<ValidateHatResponse>(validResult, HttpStatusCode.OK);
    }
}
