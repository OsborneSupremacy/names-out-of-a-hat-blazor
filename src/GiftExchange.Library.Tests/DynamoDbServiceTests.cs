using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Tests;

public class DynamoDbServiceTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDbService _sut;

    public DynamoDbServiceTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();
        var dynamoDbClient = dbFixture.CreateClient();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _sut = serviceProvider.GetRequiredService<DynamoDbService>();
    }

    [Fact]
    public async Task CreateHatAsync_GivenValidPayload_ShouldCreateItemInDynamoDb()
    {
        // arrange
        var hat = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            OrganizerName = "Test Organizer",
            OrganizerEmail = "test@test.org",
            HatName = "Test Hat",
            AdditionalInformation = "This is a test hat.",
            PriceRange = "$10 - $20",
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        // act
        var result = await _sut.CreateHatAsync(hat);

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganizerHatsAsync_GivenExistingOrganizerEmail_ShouldReturnHats()
    {
        // arrange
        var hatOne = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            OrganizerName = "Barney Organizer",
            OrganizerEmail = "barney@test.org",
            HatName = "Test Hat One",
            AdditionalInformation = "This is a test hat.",
            PriceRange = "$10 - $20",
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        var hatTwo = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            OrganizerName = "Barney Organizer",
            OrganizerEmail = "barney@test.org",
            HatName = "Test Hat Two",
            AdditionalInformation = "This is another test hat.",
            PriceRange = "$10 - $20",
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        await _sut.CreateHatAsync(hatOne);
        await _sut.CreateHatAsync(hatTwo);

        // act
        var result = await _sut.GetHatsAsync("barney@test.org");

        // assert
        result.Should().BeEquivalentTo([
            new HatMetaData { HatId = hatOne.HatId, HatName = hatOne.HatName },
            new HatMetaData { HatId = hatTwo.HatId, HatName = hatTwo.HatName }
        ]);
    }

}
