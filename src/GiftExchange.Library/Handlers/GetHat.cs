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
        var organizerEmail = request.QueryStringParameters["email"] ?? string.Empty;
        var hatParameter = request.QueryStringParameters["id"];
        var showPickedRecipients = bool.TryParse(request.QueryStringParameters["showPickedRecipients"], out var boolOut) && boolOut;

        var hatId = Guid.TryParse(hatParameter, out var guidOut) ? guidOut : Guid.Empty;

        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(organizerEmail, hatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return ApiGatewayProxyResponses.NotFound;

        if(!showPickedRecipients)
            hat = RedactPickedRecipients(hat);

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Headers = CorsHeaderService.GetCorsHeaders(),
            Body = JsonService.SerializeDefault(hat)
        };
    }

    private Hat RedactPickedRecipients(Hat hat) =>
        hat with
        {
            Participants = hat.Participants
                .Select(p => p with
                {
                    PickedRecipient = p.PickedRecipient == Persons.Empty ? Persons.Empty : Persons.Reacted
                })
                .ToImmutableList()
        };
}
