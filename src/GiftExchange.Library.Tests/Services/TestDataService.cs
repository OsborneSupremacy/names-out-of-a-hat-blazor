using GiftExchange.Library.DataModels;

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

    public Task<bool> CreateHatAsync(HatDataModel newHat) =>
        _dynamoDbService.CreateHatAsync(newHat);

    public async Task<Hat> CreateTestHatAsync()
    {
        var newHat = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            HatName = "Test Gift Exchange",
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            OrganizerEmail = "organizer@test.com",
            OrganizerName = "Organizer Test",
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

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
}
