namespace GiftExchange.Library.Services;

/// <summary>
/// Changes the display name the organizer is known by. The name lives in two places — on each of
/// their hats, and on their own participant row within those hats — so both move together.
/// </summary>
[UsedImplicitly]
internal class UpdateProfileService : IApiGatewayHandler
{
    private readonly ApiGatewayAdapter _adapter;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly IContentModerationService _contentModerationService;

    public UpdateProfileService(
        ApiGatewayAdapter adapter,
        GiftExchangeProvider giftExchangeProvider,
        IContentModerationService contentModerationService
    )
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _contentModerationService = contentModerationService ?? throw new ArgumentNullException(nameof(contentModerationService));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context) =>
        _adapter.AdaptAsync<UpdateProfileRequest, StatusCodeOnlyResponse>(request, ExecuteAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(UpdateProfileRequest request)
    {
        var (isAcceptable, moderationErrors) = await _contentModerationService
            .ValidateMultipleFieldsAsync(new Dictionary<string, string> { ["name"] = request.Name })
            .ConfigureAwait(false);

        if (!isAcceptable)
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(string.Join(" ", moderationErrors)),
                HttpStatusCode.BadRequest);

        // Participants within a hat must have distinct names, so renaming into a name somebody
        // else already uses would break that invariant for exchanges that are already set up.
        var conflicts = await _giftExchangeProvider
            .FindHatsWhereParticipantNameIsTakenAsync(request.OrganizerEmail, request.Name)
            .ConfigureAwait(false);

        if (conflicts.Any())
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(
                    $"Somebody else already goes by that name in {string.Join(", ", conflicts)}. Participants in a gift exchange need distinct names."),
                HttpStatusCode.Conflict);

        await _giftExchangeProvider
            .UpdateOrganizerNameAsync(request.OrganizerEmail, request.Name)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK },
            HttpStatusCode.OK);
    }
}
