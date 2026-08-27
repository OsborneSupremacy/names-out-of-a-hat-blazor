using System.Web;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace GiftExchange.Library.Services;

/// <summary>
/// Issues a magic link. Always reports success regardless of what happened internally, so the
/// endpoint cannot be used to probe which addresses are known.
/// </summary>
[UsedImplicitly]
internal class RequestMagicLinkService : IApiGatewayHandler
{
    private const string SenderEmail = "donotreply@mail.namesoutofahat.com";

    private const string TestRecipient = "osborne.ben@gmail.com";

    private const string SignInUrl = "https://namesoutofahat.com/auth";

    private readonly ILogger<RequestMagicLinkService> _logger;

    private readonly ApiGatewayAdapter _adapter;

    private readonly LoginTokenProvider _loginTokenProvider;

    private readonly IAmazonSimpleEmailService _sesClient;

    private readonly bool _liveMode;

    public RequestMagicLinkService(
        ILogger<RequestMagicLinkService> logger,
        ApiGatewayAdapter adapter,
        LoginTokenProvider loginTokenProvider,
        IAmazonSimpleEmailService sesClient
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _loginTokenProvider = loginTokenProvider ?? throw new ArgumentNullException(nameof(loginTokenProvider));
        _sesClient = sesClient ?? throw new ArgumentNullException(nameof(sesClient));
        _liveMode = EnvReader.TryGetBooleanValue("LIVE_MODE", out var boolOut) && boolOut;
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context) =>
        _adapter.AdaptAsync<RequestMagicLinkRequest, StatusCodeOnlyResponse>(request, ExecuteAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(RequestMagicLinkRequest request)
    {
        var accepted = new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.Accepted },
            HttpStatusCode.Accepted
        );

        if (!await _loginTokenProvider.TryReserveRequestSlotAsync(request.Email).ConfigureAwait(false))
        {
            _logger.LogInformation("Suppressed a magic link request that arrived inside the throttle window.");
            return accepted;
        }

        var token = await _loginTokenProvider
            .CreateLoginTokenAsync(request.Email)
            .ConfigureAwait(false);

        // Same test-mode redirect the invitation handler uses. Worth extracting to one place if
        // this design goes ahead.
        var recipient = _liveMode ? request.Email : TestRecipient;

        var link = $"{SignInUrl}?token={HttpUtility.UrlEncode(token)}";

        var sendRequest = new SendEmailRequest
        {
            Source = SenderEmail,
            Destination = new Destination { ToAddresses = [recipient] },
            Message = new Message
            {
                Subject = new Content("Your Names Out Of A Hat sign-in link" + (_liveMode ? string.Empty : " - TEST MODE")),
                Body = new Body
                {
                    Html = new Content(
                        $"""
                         {EmailBranding.Masthead()}<br /><br />
                         Click below to sign in to Names Out Of A Hat.<br /><br />
                         <a href="{link}"><b>🎩 Sign in 🎩</b></a><br /><br />
                         This link works once and expires in 15 minutes.<br /><br />
                         If you didn't ask to sign in, you can ignore this email.
                         """
                    )
                }
            }
        };

        try
        {
            await _sesClient.SendEmailAsync(sendRequest).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Still report success: the caller must not be able to tell delivery outcomes apart.
            _logger.LogError(exception, "Failed to send a magic link email.");
        }

        return accepted;
    }
}
