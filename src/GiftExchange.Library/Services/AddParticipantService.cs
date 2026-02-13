namespace GiftExchange.Library.Services;

internal class AddParticipantService : IApiGatewayHandler
{
    private readonly ILogger<AddParticipantService> _logger;

    private readonly ApiGatewayAdapter _adapter;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly IContentModerationService _contentModerationService;

    public AddParticipantService(
        ILogger<AddParticipantService> logger,
        ApiGatewayAdapter adapter,
        GiftExchangeProvider giftExchangeProvider,
        IContentModerationService contentModerationService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _contentModerationService = contentModerationService ?? throw new ArgumentNullException(nameof(contentModerationService));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
        ) =>
        _adapter
            .AdaptAsync<AddParticipantRequest, StatusCodeOnlyResponse>(request, AddParticipantAsync);

    private async Task<Result<StatusCodeOnlyResponse>> AddParticipantAsync(AddParticipantRequest request)
    {
        // Validate content before processing
        var (isValid, errorMessage) = await _contentModerationService.ValidateContentAsync(
            request.Name,
            "participant name");

        if (!isValid)
        {
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException(errorMessage),
                HttpStatusCode.BadRequest
            );
        }

        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(
                new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound
            );

        if(hat.InvitationsQueued)
            return new Result<StatusCodeOnlyResponse>(
                new InvalidOperationException("Cannot add participants after invitations have been sent."),
                HttpStatusCode.Conflict
            );

        var existingParticipants = await _giftExchangeProvider
            .GetParticipantsAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        // Check if a participant with the same email or name already exists
        if(existingParticipants
           .Any(p => p.Person.Email.ContentEquals(request.Email) || p.Person.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            return new Result<StatusCodeOnlyResponse>(new InvalidOperationException("Participant with provided email or name already exists. Participants must have unique email addresses and names."), HttpStatusCode.Conflict);

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

        if (hat.RecipientsAssigned || hat.Status == HatStatus.NamesAssigned) // unassign recipients if they were already assigned
            tasks.Add(_giftExchangeProvider.UpdateRecipientsAssignedAsync(request.OrganizerEmail, request.HatId, false));

        await Task
            .WhenAll(tasks)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.Created},
            HttpStatusCode.Created
        );
    }
}
