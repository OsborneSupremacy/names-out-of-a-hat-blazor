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
        var hatResult = await _getHatService
            .ExecuteAsync(new GetHatRequest { HatId = request.HatId, OrganizerEmail = request.OrganizerEmail, ShowPickedRecipients = true }, context);

        if(hatResult.IsFaulted)
            return new Result<StatusCodeOnlyResponse>(hatResult.Exception, hatResult.StatusCode);

        var hat = hatResult.Value;

        // Check if a participant with the same email or name already exists
        if(hat.Participants.Any(p => p.Person.Email.ContentEquals(request.Email) || p.Person.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            return new Result<StatusCodeOnlyResponse>(new InvalidOperationException("Participant with provided email or name already exists. Participants must have unique email addresses and names."), HttpStatusCode.Conflict);

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
