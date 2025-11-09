namespace GiftExchange.Library.Handlers;

public class GetHat
{
    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public GetHat()
    {
        _dynamoDbService = new DynamoDbService();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var organizerEmail = request.PathParameters["email"] ?? string.Empty;
        var hatParameter = request.PathParameters["id"];

        var hatId = Guid.TryParse(hatParameter, out var guidOut) ? guidOut : Guid.Empty;

        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(organizerEmail, hatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return ApiGatewayProxyResponses.NotFound;

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Headers = CorsHeaderService.GetCorsHeaders(),
            Body = JsonService.SerializeDefault(hat)
        };
    }
}
