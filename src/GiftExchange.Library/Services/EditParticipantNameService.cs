namespace GiftExchange.Library.Services;

/// <summary>
/// Changes the name one participant is known by.
/// </summary>
/// <remarks>
/// Its own endpoint rather than part of <see cref="EditParticipantService"/>, which edits
/// eligibility and resets the hat to IN_PROGRESS as it does — the same reason
/// <see cref="EditParticipantAddressService"/> and <see cref="EditParticipantEmojiService"/> are
/// their own. A rename touches nothing the draw is made of: eligibility and picks are participant
/// ids, and the names in the domain records are read off the person row every time. Resetting a
/// shaken hat over a typo would throw away a draw for no reason at all.
///
/// Every status is valid here for that reason. The one thing an organizer is not told, because it
/// would be noise, is that email already sent still carries the old name — an invitation is a
/// message that has arrived. What is still to be written, the announcement at the end, uses
/// whatever everybody is called by then.
///
/// The reach is the part worth stating plainly, and the interface in front of this should: a name
/// belongs to the person, not to their place in one exchange, so renaming somebody here renames
/// them in every exchange they are in, including ones this organizer does not run. That is not a
/// side effect of the endpoint but of a person being one row — the same thing that lets an
/// organizer correct a name once instead of once per hat.
/// </remarks>
[UsedImplicitly]
internal class EditParticipantNameService : IApiGatewayHandler
{
    private readonly ILogger<EditParticipantNameService> _logger;

    private readonly ApiGatewayAdapter _adapter;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public EditParticipantNameService(
        ILogger<EditParticipantNameService> logger,
        ApiGatewayAdapter adapter,
        HatPreconditionValidator hatPreconditionValidator,
        GiftExchangeProvider giftExchangeProvider
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync<EditParticipantNameRequest, StatusCodeOnlyResponse>(
            request,
            EditParticipantNameAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> EditParticipantNameAsync(
        EditParticipantNameRequest request
    )
    {
        // Moderated, unlike the face next to it: this is free text an organizer typed, and every
        // other participant reads it in their invitation. The same field is moderated on the way in
        // by AddParticipantService, and an edit that skipped the check would be the way around it.
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = new Dictionary<string, string>
                {
                    { "participant name", request.Name }
                },
                ValidHatStatuses = HatStatuses.All
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<StatusCodeOnlyResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        var change = await _giftExchangeProvider
            .UpdateParticipantNameAsync(new UpdateParticipantNameRequest
            {
                HatId = request.HatId,
                ParticipantEmail = request.Email,
                Name = request.Name,
                OrganizerEmail = request.OrganizerEmail
            })
            .ConfigureAwait(false);

        if (change.Outcome != NameChangeOutcome.Changed)
            return Failure(change, request);

        _logger.LogInformation(
            "Participant {ParticipantId} in hat {HatId} was renamed.",
            change.ParticipantId,
            request.HatId);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK },
            HttpStatusCode.OK);
    }

    private static Result<StatusCodeOnlyResponse> Failure(
        UpdateParticipantNameResponse change,
        EditParticipantNameRequest request
    ) =>
        change.Outcome switch
        {
            NameChangeOutcome.ParticipantNotFound => new Result<StatusCodeOnlyResponse>(
                new KeyNotFoundException($"Participant with email `{request.Email}` not found"),
                HttpStatusCode.NotFound),

            NameChangeOutcome.NameAlreadyInExchange => new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(ConflictMessage(change, request.Name)),
                HttpStatusCode.Conflict),

            _ => new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException("The name could not be changed."),
                HttpStatusCode.InternalServerError)
        };

    /// <summary>
    /// Says where the new name is already taken, in the terms the organizer is entitled to hear it.
    /// </summary>
    /// <remarks>
    /// Their own exchanges are named, because those are the ones they can go and fix. An exchange
    /// somebody else runs is not, and is not counted either: the refusal has to be explicable, and
    /// nothing more than that is theirs to know. The two are worth telling apart — one is a message
    /// about work the organizer can do, the other about a rename they will have to make differently.
    /// </remarks>
    private static string ConflictMessage(UpdateParticipantNameResponse change, string name)
    {
        if (change.ConflictingHatNames.Count == 0)
            return $"Renaming somebody changes their name in every gift exchange they are in, and in one of the others they take part in, somebody already goes by {name}. Participants in a gift exchange need distinct names, so please pick a different one.";

        var hats = string.Join(", ", change.ConflictingHatNames);

        var elsewhere = change.ConflictsElsewhere
            ? " Somebody in another gift exchange they take part in goes by it too."
            : string.Empty;

        return $"Somebody else already goes by {name} in {hats}. Participants in a gift exchange need distinct names.{elsewhere}";
    }
}
