using AWS.Lambda.Powertools.Tracing;

namespace GiftExchange.Library.Handlers;

/// <summary>
/// API Gateway TOKEN authorizer. Validates the session JWT offline and hands the caller's email
/// down to the application through the authorizer context, so no endpoint has to trust a
/// client-supplied organizer email again.
/// </summary>
[UsedImplicitly]
public class AuthorizerHandler
{
    private IServiceProvider? _serviceProvider;
    private readonly Lock _serviceProviderLock = new();

    public AuthorizerHandler() { }

    protected AuthorizerHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private IServiceProvider GetServiceProvider()
    {
        if (_serviceProvider is not null) return _serviceProvider;
        using (_serviceProviderLock.EnterScope())
        {
            if (_serviceProvider is not null) return _serviceProvider;
            _serviceProvider = ServiceProviderBuilder.Build();
        }
        return _serviceProvider;
    }

    // Disabled rather than Error. This method's return value is an IAM policy carrying the
    // caller's email as the principal, and its exceptions are thrown while deciding whether to
    // trust a token -- both are about who somebody is, which is the one category of detail that
    // should not be sitting in trace metadata. The subsegment and its timing are still recorded;
    // only the contents are withheld.
    [Tracing(CaptureMode = TracingCaptureMode.Disabled)]
    public async Task<APIGatewayCustomAuthorizerResponse> FunctionHandler(
        APIGatewayCustomAuthorizerRequest request,
        ILambdaContext context
    )
    {
        var token = ExtractBearerToken(request.AuthorizationToken);

        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedException();

        var sessionTokenService = GetServiceProvider().GetRequiredService<SessionTokenService>();

        var (isValid, email) = await sessionTokenService
            .ValidateAsync(token)
            .ConfigureAwait(false);

        if (!isValid)
        {
            context.Logger.LogWarning("Rejected a request carrying an invalid or expired session token.");
            throw new UnauthorizedException();
        }

        return BuildAllowResponse(email, request.MethodArn);
    }

    private static string ExtractBearerToken(string? authorizationHeader) =>
        authorizationHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? authorizationHeader["Bearer ".Length..].Trim()
            : authorizationHeader?.Trim() ?? string.Empty;

    private static APIGatewayCustomAuthorizerResponse BuildAllowResponse(string email, string methodArn) =>
        new()
        {
            PrincipalID = email,
            PolicyDocument = new APIGatewayCustomAuthorizerPolicy
            {
                Version = "2012-10-17",
                Statement =
                [
                    new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement
                    {
                        Effect = "Allow",
                        Action = ["execute-api:Invoke"],
                        Resource = [BuildApiWideResource(methodArn)]
                    }
                ]
            },
            Context = new APIGatewayCustomAuthorizerContextOutput
            {
                ["email"] = email
            }
        };

    /// <summary>
    /// Widens arn:...:apiId/stage/GET/hat/{email}/{id} to arn:...:apiId/stage/*\/*, so the policy
    /// API Gateway caches for this token is valid for every endpoint rather than just the first
    /// one the caller happened to hit.
    /// </summary>
    private static string BuildApiWideResource(string methodArn)
    {
        var segments = methodArn.Split('/');
        return segments.Length < 2
            ? methodArn
            : $"{segments[0]}/{segments[1]}/*/*";
    }
}

/// <summary>
/// API Gateway maps a thrown error whose message is exactly "Unauthorized" to a 401. Anything else
/// surfaces as a 500.
/// </summary>
internal sealed class UnauthorizedException() : Exception("Unauthorized");
