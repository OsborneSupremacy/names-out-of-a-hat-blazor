namespace GiftExchange.Library.Messaging;

internal static class ApiGatewayProxyResponses
{
    public static readonly APIGatewayProxyResponse BadRequest = new()
    {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Headers = CorsHeaderService.GetCorsHeaders()
    };

    public static readonly APIGatewayProxyResponse NotFound = new()
    {
        StatusCode = (int)HttpStatusCode.NotFound,
        Headers = CorsHeaderService.GetCorsHeaders()
    };
}
