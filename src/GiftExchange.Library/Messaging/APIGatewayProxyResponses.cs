namespace GiftExchange.Library.Messaging;

internal static class ApiGatewayProxyResponses
{
    public static readonly APIGatewayProxyResponse BadRequest = new APIGatewayProxyResponse
    {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Headers = CorsHeaderService.GetCorsHeaders()
    };

}
