namespace GiftExchange.Library.Tests.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// Builds a proxy request whose authenticated caller is the organizer named in the body, which
    /// is the case every handler test is interested in. Pass <paramref name="authenticatedEmail"/>
    /// explicitly to model a caller who is somebody else.
    /// </summary>
    public static APIGatewayProxyRequest ToApiGatewayProxyRequest(
        this string body,
        string? authenticatedEmail = null
    ) =>
        new APIGatewayProxyRequest { Body = body }
            .WithAuthenticatedEmail(authenticatedEmail ?? body.GetOrganizerEmailFromBody());
}
