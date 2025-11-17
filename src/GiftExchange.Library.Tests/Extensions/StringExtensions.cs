namespace GiftExchange.Library.Tests.Extensions;

internal static class StringExtensions
{
    public static APIGatewayProxyRequest ToApiGatewayProxyRequest(this string body) =>
        new()
        {
            Body = body
        };
}
