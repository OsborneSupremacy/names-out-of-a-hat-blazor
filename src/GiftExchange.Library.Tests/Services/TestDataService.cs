
namespace GiftExchange.Library.Tests.Services;

internal class TestDataService
{
    private readonly DynamoDbService _dynamoDbService;

    private readonly HatDataModelFaker _hatDataModelFaker = new HatDataModelFaker();

    public TestDataService(
        DynamoDbService dynamoDbService
        )
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public Task<bool> CreateHatAsync(HatDataModel newHat) =>
        _dynamoDbService.CreateHatAsync(newHat);

    public async Task<Hat> CreateTestHatAsync()
    {
        var newHat = _hatDataModelFaker.Generate();

        await _dynamoDbService.CreateHatAsync(newHat);

        return new Hat
        {
            Id = newHat.HatId,
            Name = newHat.HatName,
            AdditionalInformation = newHat.AdditionalInformation,
            PriceRange = newHat.PriceRange,
            OrganizerVerified = newHat.OrganizerVerified,
            RecipientsAssigned = newHat.RecipientsAssigned,
            Organizer = new Person {
                Email = newHat.OrganizerEmail,
                Name = newHat.OrganizerName
            },
            Participants = []
        };
    }

    public async Task<Hat> GetHatAsync(string organizerEmail, Guid hatId)
    {
        var (_, hat) = await _dynamoDbService
            .GetHatAsync(organizerEmail, hatId);
        return hat;
    }

    public Task<bool> CreateParticipantAsync(
        AddParticipantRequest request,
        ImmutableList<Participant> existingParticipants
        ) => _dynamoDbService.CreateParticipantAsync(request, existingParticipants);

    public async Task<Participant> GetParticipantAsync(
        string organizerEmail,
        Guid hatId,
        string participantUtEmail
        )
    {
        var (_, participant) = await _dynamoDbService
            .GetParticipantAsync(organizerEmail, hatId, participantUtEmail);
        return participant;
    }
}
