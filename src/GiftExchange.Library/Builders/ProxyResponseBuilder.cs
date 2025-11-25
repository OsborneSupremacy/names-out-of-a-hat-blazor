namespace GiftExchange.Library.Builders;

public static class ProxyResponseBuilder
{
    public static APIGatewayProxyResponse Build(HttpStatusCode statusCode)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = CorsHeaderProvider.GetCorsHeaders()
        };
    }

    public static APIGatewayProxyResponse Build(HttpStatusCode statusCode, string body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = body,
            Headers = CorsHeaderProvider.GetCorsHeaders()
        };
    }
}
