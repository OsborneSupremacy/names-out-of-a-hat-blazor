namespace GiftExchange.Library.Services;

public class AddParticipantService : IBusinessService<AddParticipantRequest, StatusCodeOnlyResponse>
{
    private readonly ILogger<AddParticipantService> _logger;

    private readonly GetHatService _getHatService;

    private readonly DynamoDbService _dynamoDbService;

    public AddParticipantService(
        ILogger<AddParticipantService> logger,
        DynamoDbService dynamoDbService,
        GetHatService getHatService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
        _getHatService = getHatService ?? throw new ArgumentNullException(nameof(getHatService));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(AddParticipantRequest request, ILambdaContext context)
    {
        var (hatExists, _ ) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(
                new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound
            );

        var existingParticipants = await _dynamoDbService
            .GetParticipantsAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        // Check if a participant with the same email or name already exists
        if(existingParticipants.Any(p => p.Person.Email.ContentEquals(request.Email) || p.Person.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            return new Result<StatusCodeOnlyResponse>(new InvalidOperationException("Participant with provided email or name already exists. Participants must have unique email addresses and names."), HttpStatusCode.Conflict);

        await _dynamoDbService
            .CreateParticipantAsync(new AddParticipantRequest
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
                _dynamoDbService
                    .AddParticipantEligibleRecipientAsync(
                        request.OrganizerEmail,
                        request.HatId,
                        participant.Person.Email,
                        request.Name
                    ))
            .ToList();

        await Task.WhenAll(tasks)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.Created},
            HttpStatusCode.Created
        );
    }
}
