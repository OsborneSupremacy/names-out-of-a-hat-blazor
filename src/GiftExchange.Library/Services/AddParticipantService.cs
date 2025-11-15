namespace GiftExchange.Library.Services;

public class AddParticipantService : IBusinessService<AddParticipantRequest, StatusCodeOnlyResponse>
{
    private readonly ILogger<AddParticipantService> _logger;

    private readonly DynamoDbService _dynamoDbService;

    public AddParticipantService(ILogger<AddParticipantService> logger, DynamoDbService dynamoDbService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(AddParticipantRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        // Check if a participant with the same email already exists
        if(hat.Participants.Any(p => p.Person.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            return new Result<StatusCodeOnlyResponse>(new InvalidOperationException($"Participant with email {request.Email} already exists in the hat."), HttpStatusCode.Conflict);

        var newPerson = new Person
        {
            Name = request.Name,
            Email = request.Email,
        };

        var newParticipant = new Participant
        {
            Person = newPerson,
            PickedRecipient = Persons.Empty,
            Recipients = hat.Participants
                .Select(p => new Recipient
                {
                    Person = p.Person,
                    Eligible = true
                }).ToImmutableList()
        };

        var participantsOut = hat.Participants
            .Select(p => p with
            {
                Recipients = p.Recipients
                    .Concat([new Recipient
                    {
                        Person = newPerson,
                        Eligible = true
                    }])
                    .ToImmutableList()
            })
            .Concat([newParticipant])
            .ToImmutableList();

        await _dynamoDbService.UpdateParticipantsAsync(
            request.OrganizerEmail,
            request.HatId,
            participantsOut
        ) .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(
            new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.Created},
            HttpStatusCode.Created
        );
    }
}
