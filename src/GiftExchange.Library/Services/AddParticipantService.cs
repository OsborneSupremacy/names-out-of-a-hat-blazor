namespace GiftExchange.Library.Services;

internal class AddParticipantService : IApiGatewayHandler
{
    private readonly ILogger<AddParticipantService> _logger;

    private readonly ApiGatewayAdapter _adapter;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly DoNotAddService _doNotAddService;

    public AddParticipantService(
        ILogger<AddParticipantService> logger,
        ApiGatewayAdapter adapter,
        HatPreconditionValidator hatPreconditionValidator,
        GiftExchangeProvider giftExchangeProvider,
        DoNotAddService doNotAddService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _doNotAddService = doNotAddService ?? throw new ArgumentNullException(nameof(doNotAddService));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
        ) =>
        _adapter
            .AdaptAsync<AddParticipantRequest, StatusCodeOnlyResponse>(request, AddParticipantAsync);

    private async Task<Result<StatusCodeOnlyResponse>> AddParticipantAsync(AddParticipantRequest request)
    {
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId =  request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = new Dictionary<string, string>
                {
                    { "participant name", request.Name }
                },
                ValidHatStatuses = [ HatStatus.InProgress, HatStatus.ReadyForAssignment, HatStatus.NamesAssigned]
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<StatusCodeOnlyResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode
            );

        var hat = hatPreconditionResult.Hat;

        var existingParticipants = await _giftExchangeProvider
            .GetParticipantsAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        // Check if a participant with the same email or name already exists
        if(existingParticipants
           .Any(p => p.Person.Email.ContentEquals(request.Email) || p.Person.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            return new Result<StatusCodeOnlyResponse>(new InvalidOperationException("Participant with provided email or name already exists. Participants must have unique email addresses and names."), HttpStatusCode.Conflict);

        // After the duplicate check and before the write. Somebody who has refused this exchange, or
        // this organizer, or all of them, is not added back by an organizer typing their address in
        // again — which is the ordinary next thing to happen, since the organizer is told somebody
        // left and asked to draw names afresh.
        //
        // Forbidden rather than Conflict. A conflict says the request collided with something and
        // could be retried differently; this one cannot be retried at all with this address.
        var refused = await _doNotAddService
            .IsRefusedAsync(request.Email, request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if (refused)
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(DoNotAddService.RefusalMessage),
                HttpStatusCode.Forbidden);

        await _giftExchangeProvider
            .CreateParticipantAsync(
                new AddParticipantRequest
                {
                    OrganizerEmail = request.OrganizerEmail,
                    HatId = request.HatId,
                    Name = request.Name,
                    Email = request.Email
                }, existingParticipants)
            .ConfigureAwait(false);

        // make new participant eligible for all existing participants
        var tasks = existingParticipants
            .Select(participant =>
                _giftExchangeProvider
                    .AddParticipantEligibleRecipientAsync(
                        request.OrganizerEmail,
                        request.HatId,
                        participant.Person.Email,
                        request.Name
                    ))
            .ToList();

        if (hat.Status != HatStatus.InProgress)
            tasks.Add(
                _giftExchangeProvider
                    .UpdateHatStatusAsync(request.OrganizerEmail, request.HatId, HatStatus.InProgress)
                );

        await Task
            .WhenAll(tasks)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.Created},
            HttpStatusCode.Created
        );
    }
}
