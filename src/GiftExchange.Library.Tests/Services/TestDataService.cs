namespace GiftExchange.Library.Tests.Services;

internal class TestDataService
{
    private readonly DynamoDbService _dynamoDbService;

    public TestDataService(
        DynamoDbService dynamoDbService
        )
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Hat> CreateTestHatAsync()
    {
        var newHat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = "Test Gift Exchange",
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = new Person
            {
                Email = "organizer@test.com",
                Name = "Organizer Test"
            },
            Participants = [
                new Participant
                {
                    Person = new Person
                    {
                        Email = "organizer@test.com",
                        Name = "Organizer Test"
                    },
                    Recipients = [],
                    PickedRecipient = Persons.Empty
                }
            ],
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        await _dynamoDbService.CreateHatAsync(newHat);

        return newHat;
    }
}
