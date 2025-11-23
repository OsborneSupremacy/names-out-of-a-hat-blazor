using Amazon.DynamoDBv2.Model;

namespace GiftExchange.Library.Tests;

public class GiftExchangeDataProviderTests : IClassFixture<DynamoDbFixture>
{
    private readonly GiftExchangeDataProvider _sut;

    private readonly HatDataModelFaker _hatDataModelFaker;

    private readonly AddParticipantRequestFaker _addParticipantRequestFaker;

    public GiftExchangeDataProviderTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        _hatDataModelFaker = new HatDataModelFaker();
        _addParticipantRequestFaker = new AddParticipantRequestFaker();

        var dynamoDbClient = dbFixture.CreateClient();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _sut = serviceProvider.GetRequiredService<GiftExchangeDataProvider>();
    }

    [Fact]
    public async Task CreateHatAsync_GivenValidPayload_ShouldCreateItemInDynamoDb()
    {
        // arrange
        var hat = _hatDataModelFaker.Generate();

        // act
        var result = await _sut.CreateHatAsync(hat);

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganizerHatsAsync_GivenExistingOrganizerEmail_ShouldReturnHats()
    {
        // arrange
        var hats = _hatDataModelFaker.Generate(2);

        var hatOne = hats[0];
        var hatTwo = hats[1] with
        {
            OrganizerEmail = hatOne.OrganizerEmail,
        };

        await _sut.CreateHatAsync(hatOne);
        await _sut.CreateHatAsync(hatTwo);

        // act
        var result = await _sut.GetHatsAsync(hatOne.OrganizerEmail);

        // assert
        result.Should().BeEquivalentTo([
            new HatMetaData { HatId = hatOne.HatId, HatName = hatOne.HatName },
            new HatMetaData { HatId = hatTwo.HatId, HatName = hatTwo.HatName }
        ]);
    }

    [Fact]
    public async Task CreateParticipantAsync_GivenValidPayload_ShouldCreateItemInDynamoDb()
    {
        // arrange
        var request =_addParticipantRequestFaker.Generate();

        // act
        var result = await _sut.CreateParticipantAsync(request, []);

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateParticipantAsync_GivenDuplicatePayload_ShouldThrowException()
    {
        // arrange
        var request = _addParticipantRequestFaker.Generate();

        // act
        var firstRequest = await _sut.CreateParticipantAsync(request, []);

        Func<Task> secondRequest = async () =>
        {
            await _sut.CreateParticipantAsync(request, []);
        };

        // assert
        firstRequest.Should().BeTrue();
        await secondRequest.Should().ThrowAsync<ConditionalCheckFailedException>();
    }

    [Fact]
    public async Task GetParticipantAsync_GivenExistingParticipants_ShouldReturnParticipants()
    {
        // arrange
        var hatId = Guid.NewGuid();
        var organizerEmail = new Bogus.Person().Email;

        foreach (var participant in _addParticipantRequestFaker.Generate(3))
            await _sut.CreateParticipantAsync(participant with
            {
                HatId = hatId,
                OrganizerEmail = organizerEmail
            }, []);

        // act
        var result = await _sut.GetParticipantsAsync(organizerEmail, hatId);

        // assert
        result.Count.Should().Be(3);
    }
}
