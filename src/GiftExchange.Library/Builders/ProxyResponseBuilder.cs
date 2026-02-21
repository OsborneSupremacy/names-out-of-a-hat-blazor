namespace GiftExchange.Library.Builders;

public static class ProxyResponseBuilder
{
    public static APIGatewayProxyResponse Build(HttpStatusCode statusCode) =>
        new()
        {
            StatusCode = (int)statusCode,
            Headers = CorsHeaderProvider.GetCorsHeaders()
        };

    public static APIGatewayProxyResponse Build(HttpStatusCode statusCode, string body) =>
        new()
        {
            StatusCode = (int)statusCode,
            Body = body,
            Headers = CorsHeaderProvider.GetCorsHeaders()
        };
}
