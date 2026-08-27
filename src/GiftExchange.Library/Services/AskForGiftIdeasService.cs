namespace GiftExchange.Library.Services;

/// <summary>
/// The Ask: one participant asking the person they drew for gift ideas, without being named.
/// </summary>
/// <remarks>
/// Two endpoints for one action, and the split is the security design rather than an accident of
/// REST. The button lives in an email, so following it is a GET — and mail security scanners,
/// Microsoft Defender Safe Links among them, fetch links in delivered mail to check them. A GET
/// that sent the Ask would therefore fire on delivery for a large share of recipients: their
/// throttle window spent, and somebody mailed on behalf of a person who had not yet read the
/// invitation, let alone clicked anything.
///
/// So the GET only renders a page asking whether they meant it, which a scanner is welcome to
/// fetch as often as it likes, and the POST behind the button on that page does the work.
/// </remarks>
[UsedImplicitly]
internal class AskForGiftIdeasService : IApiGatewayHandler
{
    /// <summary>
    /// How long a participant has to wait before asking the same person again.
    ///
    /// A week, because the thing being asked for takes days to think about, and because the person
    /// on the receiving end cannot tell repeated asks from nagging — they do not know how many
    /// people are asking, only how often they are being asked. Short enough that somebody who
    /// genuinely got no answer can try again within the life of an exchange.
    /// </summary>
    private static readonly TimeSpan AskWindow = TimeSpan.FromDays(7);

    /// <summary>Statuses during which there is still somebody to ask.</summary>
    private static readonly ImmutableList<string> AskableStatuses =
        [HatStatus.NamesAssigned, HatStatus.InvitationsSent, HatStatus.CooledOff];

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly IReplyThrottleProvider _replyThrottleProvider;

    private readonly GiftIdeaEmailCompositionService _composer;

    private readonly AskPageComposer _pageComposer;

    private readonly AutomaticEmailSender _sender;

    private readonly ILogger<AskForGiftIdeasService> _logger;

    public AskForGiftIdeasService(
        GiftExchangeProvider giftExchangeProvider,
        IReplyThrottleProvider replyThrottleProvider,
        GiftIdeaEmailCompositionService composer,
        AskPageComposer pageComposer,
        AutomaticEmailSender sender,
        ILogger<AskForGiftIdeasService> logger
    )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _replyThrottleProvider = replyThrottleProvider ?? throw new ArgumentNullException(nameof(replyThrottleProvider));
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        _pageComposer = pageComposer ?? throw new ArgumentNullException(nameof(pageComposer));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        // Case preserved. The token is base64url, so "aB" and "Ab" are different tokens, and the
        // ordinary instinct to normalise an identifier from a URL would break every Ask.
        var token = request.PathParameters is not null
            && request.PathParameters.TryGetValue("token", out var pathToken)
                ? pathToken
                : string.Empty;

        var (found, route) = await _giftExchangeProvider
            .FindGiftIdeaRouteAsync(SecretToken.Hash(token))
            .ConfigureAwait(false);

        // Deliberately the same page for an unknown token and a finished exchange. Telling the
        // difference apart would let somebody holding a guessed token learn whether it named a real
        // participant.
        if (!found || !AskableStatuses.Contains(route.HatStatus))
            return Page(_pageComposer.ComposeUnavailable());

        if (route.SenderPickedRecipientParticipantId == Guid.Empty
            || string.IsNullOrWhiteSpace(route.SenderPickedRecipient.Email))
            return Page(_pageComposer.ComposeUnavailable());

        return request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase)
            ? await SendAskAsync(route).ConfigureAwait(false)
            : Page(_pageComposer.ComposeConfirm(route.SenderPickedRecipient.Name, token));
    }

    private async Task<APIGatewayProxyResponse> SendAskAsync(GiftIdeaRoute route)
    {
        var (reserved, previouslyAskedAt) = await _replyThrottleProvider
            .TryReserveAskSlotAsync(route.ParticipantId, AskWindow)
            .ConfigureAwait(false);

        // Refused asks are answered by email rather than only on the page, because the likeliest
        // reader is somebody who does not remember asking and needs the date to make sense of it —
        // and because the page is gone the moment they close the tab.
        if (!reserved)
        {
            _logger.LogInformation("Suppressed an Ask inside the throttle window.");

            await _sender.SendAsync(
                    route.Sender.Email,
                    GiftIdeaEmailCompositionService.AskThrottledSubject,
                    _composer.ComposeAskThrottled(route.SenderPickedRecipient.Name, previouslyAskedAt))
                .ConfigureAwait(false);

            return Page(_pageComposer.ComposeAlreadyAsked(route.SenderPickedRecipient.Name, previouslyAskedAt));
        }

        // A token of their own, issued alongside any they already hold rather than over them.
        // Theirs cannot be reconstructed — only its hash was kept — so this is the only way to put
        // a working SHARE GIFT IDEAS address into an email they did not originally receive.
        var giftIdeasToken = await _giftExchangeProvider
            .IssueGiftIdeaTokenAsync(route.SenderPickedRecipientParticipantId)
            .ConfigureAwait(false);

        // Names nobody. Everybody is drawn by exactly one person, so being asked reveals nothing
        // the recipient did not already know — but naming the asker would reveal the one thing
        // this application exists to keep quiet.
        await _sender.SendAsync(
                route.SenderPickedRecipient.Email,
                GiftIdeaEmailCompositionService.AskSubject(route.HatName),
                _composer.ComposeAsk(route.HatName, giftIdeasToken))
            .ConfigureAwait(false);

        return Page(_pageComposer.ComposeSent(route.SenderPickedRecipient.Name));
    }

    /// <summary>
    /// Every outcome is a 200 carrying a page, including the ones that did nothing.
    /// </summary>
    /// <remarks>
    /// A status code would be read by the scanner that fetched this before any person did, and
    /// there is nobody for a 404 to inform. The reader is a human looking at a browser tab, so the
    /// page says what happened and the code stays out of it.
    /// </remarks>
    private static APIGatewayProxyResponse Page(string html) =>
        new()
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = html,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "text/html; charset=utf-8",
                // Nothing here is worth storing, and a cached Ask page shown after the fact would
                // report an outcome that is no longer true.
                ["Cache-Control"] = "no-store"
            }
        };
}
