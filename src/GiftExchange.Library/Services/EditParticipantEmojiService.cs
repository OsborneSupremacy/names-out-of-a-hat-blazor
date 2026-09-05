namespace GiftExchange.Library.Services;

/// <summary>
/// Changes the face one participant is marked with.
/// </summary>
/// <remarks>
/// Its own endpoint rather than part of <see cref="EditParticipantService"/>, which edits
/// eligibility and resets the hat to IN_PROGRESS as it does. That reset is right for a change to
/// who may draw whom and absurd for a change of face — it would throw away a completed draw over
/// decoration.
///
/// Every status is valid here for the same reason: there is nothing about a face that a shaken or
/// finished exchange makes dangerous to change. What an organizer is not told, because it would be
/// noise, is that email already sent still carries the old face. The invitation is a message that
/// has arrived; only the announcement at the end is still to be written, and it will use whatever
/// the participant is wearing by then.
/// </remarks>
[UsedImplicitly]
internal class EditParticipantEmojiService : IApiGatewayHandler
{
    private readonly ApiGatewayAdapter _adapter;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public EditParticipantEmojiService(
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
        _adapter.AdaptAsync<EditParticipantEmojiRequest, StatusCodeOnlyResponse>(
            request,
            EditParticipantEmojiAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> EditParticipantEmojiAsync(
        EditParticipantEmojiRequest request
    )
    {
        // Nothing to moderate: the face is one of a closed list, which the validator has already
        // established. The precondition check is here for what it always establishes — that this
        // hat exists and belongs to the caller.
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = [],
                ValidHatStatuses = HatStatuses.All
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<StatusCodeOnlyResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        var changed = await _giftExchangeProvider
            .UpdateParticipantEmojiAsync(new UpdateParticipantEmojiRequest
            {
                HatId = request.HatId,
                ParticipantEmail = request.Email,
                Emoji = request.Emoji
            })
            .ConfigureAwait(false);

        if (!changed)
            return new Result<StatusCodeOnlyResponse>(
                new KeyNotFoundException($"Participant with email `{request.Email}` not found"),
                HttpStatusCode.NotFound);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK },
            HttpStatusCode.OK);
    }
}
