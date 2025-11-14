namespace GiftExchange.Library.Services;

public class CreateHatService : IServiceWithResponseBody<CreateHatRequest, CreateHatResponse>
{
    private readonly DynamoDbService _dynamoDbService;

    public CreateHatService(DynamoDbService dynamoDbService)
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result<CreateHatResponse>> ExecuteAsync(CreateHatRequest request, ILambdaContext context)
    {
        var (hatExists, hatId) = await _dynamoDbService
            .DoesHatExistAsync(request.OrganizerEmail)
            .ConfigureAwait(false);

        if (hatExists)
            return new Result<CreateHatResponse>(new CreateHatResponse { HatId = hatId }, HttpStatusCode.OK);

        var organizer = new Person { Name = request.OrganizerName, Email = request.OrganizerEmail };

        var organizerParticipant = new Participant
        {
            PickedRecipient = Persons.Empty,
            Person = organizer,
            Recipients = []
        };

        var newHat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = request.HatName,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = organizer,
            Participants = [ organizerParticipant ],
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        var newHatId = await _dynamoDbService
            .CreateHatAsync(newHat)
            .ConfigureAwait(false);

        return new Result<CreateHatResponse>(new CreateHatResponse { HatId = newHatId }, HttpStatusCode.Created);
    }
}
