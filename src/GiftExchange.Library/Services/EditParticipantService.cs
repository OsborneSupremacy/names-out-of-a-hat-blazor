namespace GiftExchange.Library.Services;

[UsedImplicitly]
internal class EditParticipantService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly ApiGatewayAdapter _adapter;


    public EditParticipantService(
        GiftExchangeProvider giftExchangeProvider,
        HatPreconditionValidator hatPreconditionValidator,
        ApiGatewayAdapter adapter
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync<EditParticipantRequest, StatusCodeOnlyResponse>(request, EditParticipantAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> EditParticipantAsync(
        EditParticipantRequest request
        )
    {
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = [],
                ValidHatStatuses = [HatStatus.InProgress, HatStatus.ReadyForAssignment, HatStatus.NamesAssigned]
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<StatusCodeOnlyResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        var hat = hatPreconditionResult.Hat;

        var (participantExists, participant) = await _giftExchangeProvider
            .GetParticipantAsync(
                request.OrganizerEmail,
                request.HatId,
                request.Email
            )
            .ConfigureAwait(false);

        if(!participantExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Participant with email `{request.Email}` not found"), HttpStatusCode.NotFound);

        if(request.EligibleRecipients.Count == 0)
            return new Result<StatusCodeOnlyResponse>(new ArgumentException("Participant must have at least one eligible recipient"), HttpStatusCode.BadRequest);

        if(request.EligibleRecipients.Contains(participant.Person.Name, StringComparer.OrdinalIgnoreCase))
            return new Result<StatusCodeOnlyResponse>(new ArgumentException("Participant cannot set themselves as an eligible recipient"), HttpStatusCode.BadRequest);

        var otherParticipants = hat.Participants
            .Where(p => !p.Person.Name.ContentEquals(participant.Person.Name))
            .Select(p => p.Person.Name)
            .ToImmutableList();

        var invalidRecipients = request.EligibleRecipients
            .Where(r => !otherParticipants.Contains(r, StringComparer.OrdinalIgnoreCase))
            .ToImmutableList();

        if (invalidRecipients.Any())
        {
            var errorMessage = $"""
                                One or more provided recipients are not part of this gift exchange.

                                Gift exchange participants: {string.Join(", ", otherParticipants)}
                                Provided Recipients: {string.Join(", ", request.EligibleRecipients)}
                                Invalid Recipients: {string.Join(", ", invalidRecipients)}


                                """;

            return new Result<StatusCodeOnlyResponse>(new ArgumentException(errorMessage), HttpStatusCode.BadRequest);
        }

        await _giftExchangeProvider
            .UpdateEligibleRecipientsAsync(
                request.OrganizerEmail,
                request.HatId,
                request.Email,
                request.EligibleRecipients
            )
            .ConfigureAwait(false);

        if (hat.RecipientsAssigned || hat.Status == HatStatus.NamesAssigned) // unassign recipients if they were already assigned
            await _giftExchangeProvider.UpdateRecipientsAssignedAsync(request.OrganizerEmail, request.HatId, false)
                .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
