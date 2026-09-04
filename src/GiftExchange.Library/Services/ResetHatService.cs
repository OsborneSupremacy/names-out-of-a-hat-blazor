namespace GiftExchange.Library.Services;

/// <summary>
/// Takes a gift exchange back to the beginning without emptying it: the same people stay, everybody
/// is allowed to draw everybody again, and nobody is holding a name.
/// </summary>
/// <remarks>
/// Only before invitations go out. After that, people have been told who they drew, and undoing the
/// draw would make what they were told wrong with no way to tell them so — which is why this is not
/// simply a copy that reuses the same row.
///
/// The people are what a reset keeps, and they are the part that took work to type in. Everything
/// else about an exchange is either quick to state again or the thing being thrown away on purpose.
/// </remarks>
internal class ResetHatService : IApiGatewayHandler
{
    private const string RaceMessage =
        "This gift exchange moved on while it was being reset, so nothing was changed. Reload it to see where it has got to.";

    private readonly ApiGatewayAdapter _adapter;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public ResetHatService(
        ApiGatewayAdapter adapter,
        HatPreconditionValidator hatPreconditionValidator,
        GiftExchangeProvider giftExchangeProvider
    )
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync<ResetHatRequest, StatusCodeOnlyResponse>(request, ResetHatAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> ResetHatAsync(ResetHatRequest request)
    {
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = [],
                ValidHatStatuses = HatStatuses.BeforeInvitationsSent
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<StatusCodeOnlyResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        // The provider checks the status again inside its transaction, so a send that landed
        // between that validation and this call leaves the exchange alone and says so.
        var wasReset = await _giftExchangeProvider
            .ResetHatAsync(request)
            .ConfigureAwait(false);

        if (!wasReset)
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(RaceMessage),
                HttpStatusCode.Conflict);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK },
            HttpStatusCode.OK);
    }
}
