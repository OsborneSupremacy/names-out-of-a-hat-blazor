namespace GiftExchange.Library.Services;

/// <summary>
/// Exchanges a single-use magic link token for a session JWT.
/// </summary>
[UsedImplicitly]
internal class RedeemMagicLinkService : IApiGatewayHandler
{
    private readonly ApiGatewayAdapter _adapter;

    private readonly LoginTokenProvider _loginTokenProvider;

    private readonly SessionTokenService _sessionTokenService;

    public RedeemMagicLinkService(
        ApiGatewayAdapter adapter,
        LoginTokenProvider loginTokenProvider,
        SessionTokenService sessionTokenService
    )
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _loginTokenProvider = loginTokenProvider ?? throw new ArgumentNullException(nameof(loginTokenProvider));
        _sessionTokenService = sessionTokenService ?? throw new ArgumentNullException(nameof(sessionTokenService));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context) =>
        _adapter.AdaptAsync<RedeemMagicLinkRequest, RedeemMagicLinkResponse>(request, ExecuteAsync);

    internal async Task<Result<RedeemMagicLinkResponse>> ExecuteAsync(RedeemMagicLinkRequest request)
    {
        var (redeemed, email) = await _loginTokenProvider
            .TryRedeemLoginTokenAsync(request.Token)
            .ConfigureAwait(false);

        if (!redeemed)
            return new Result<RedeemMagicLinkResponse>(
                new UnauthorizedAccessException("That sign-in link is no longer valid. Please request a new one."),
                HttpStatusCode.Unauthorized
            );

        var (accessToken, expiresAt) = await _sessionTokenService
            .IssueAsync(email)
            .ConfigureAwait(false);

        return new Result<RedeemMagicLinkResponse>(
            new RedeemMagicLinkResponse
            {
                AccessToken = accessToken,
                Email = email,
                ExpiresAt = expiresAt
            },
            HttpStatusCode.OK
        );
    }
}
