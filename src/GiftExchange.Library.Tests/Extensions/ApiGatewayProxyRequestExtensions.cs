using System.Text.Json;

namespace GiftExchange.Library.Tests.Extensions;

internal static class ApiGatewayProxyRequestExtensions
{
    /// <summary>
    /// Stands in for what the Lambda authorizer supplies in production. Handlers read the
    /// organizer's identity from here, never from the request body or path.
    /// </summary>
    public static APIGatewayProxyRequest WithAuthenticatedEmail(
        this APIGatewayProxyRequest request,
        string email
    )
    {
        request.RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
        {
            Authorizer = new APIGatewayCustomAuthorizerContext
            {
                ["email"] = email
            }
        };
        return request;
    }

    /// <summary>
    /// Reads organizerEmail out of a serialized request body, so a test that already sets it does
    /// not have to state the authenticated caller twice.
    /// </summary>
    public static string GetOrganizerEmailFromBody(this string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;

        using var document = JsonDocument.Parse(body);

        return document.RootElement.TryGetProperty("organizerEmail", out var organizerEmail)
            ? organizerEmail.GetString() ?? string.Empty
            : string.Empty;
    }
}
