namespace GiftExchange.Library.Services;

/// <summary>
/// Changes the display name the caller is known by.
/// </summary>
/// <remarks>
/// The same write <see cref="EditParticipantNameService"/> makes, through the same provider method,
/// and the difference between the two is only whose address is being renamed. It used to be its own
/// implementation, and the two drifted in the way two spellings of one fact do: this one refused a
/// name already taken in an exchange the caller organizes, and said nothing about one taken in an
/// exchange somebody else does — which a rename reaches just as surely, because a name belongs to
/// the person.
///
/// Nothing here can be refused for want of standing. A person may always change their own name,
/// which is the first of the two rules <c>PersonEntity.AddedByPersonId</c> exists to express, so
/// the Forbidden the other endpoint can return is unreachable from this one.
/// </remarks>
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

        var change = await _giftExchangeProvider
            .RenamePersonAsync(new RenamePersonRequest
            {
                Email = request.OrganizerEmail,
                Name = request.Name,
                RequestedByEmail = request.OrganizerEmail
            })
            .ConfigureAwait(false);

        // Participants within an exchange must have distinct names, and a rename is felt in every
        // exchange the person is in — so the collision that refuses this one may be in an exchange
        // they take part in rather than one they run. Named where the caller could act on it, and
        // acknowledged without being named where they could not.
        if (change.Outcome == NameChangeOutcome.NameAlreadyInExchange)
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(ConflictMessage(change)),
                HttpStatusCode.Conflict);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK },
            HttpStatusCode.OK);
    }

    private static string ConflictMessage(RenamePersonResponse change)
    {
        if (change.ConflictingHatNames.Count == 0)
            return "Changing your name changes it in every gift exchange you are in, and somebody in one of the ones you take part in already goes by that name. Participants in a gift exchange need distinct names, so please pick a different one.";

        var hats = string.Join(", ", change.ConflictingHatNames);

        var elsewhere = change.ConflictsElsewhere
            ? " Somebody in a gift exchange you take part in goes by it too."
            : string.Empty;

        return $"Somebody else already goes by that name in {hats}. Participants in a gift exchange need distinct names.{elsewhere}";
    }
}
