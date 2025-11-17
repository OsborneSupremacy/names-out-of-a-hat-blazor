using Amazon.DynamoDBv2;

namespace GiftExchange.Library.Tests;

public class AddParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly IServiceProvider _serviceProvider;

    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly DynamoDbService _dynamoDbService;

    private readonly IAmazonDynamoDB _dynamoDbClient;

    public AddParticipantTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        _dynamoDbClient = dbFixture.CreateClient();

        _context = new FakeLambdaContext();
        _jsonService = new JsonService();
        _serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(_dynamoDbClient)
            .BuildServiceProvider();

        _dynamoDbService = _serviceProvider.GetRequiredService<DynamoDbService>();
    }

    [Fact]
    public async Task AddParticipant_ValidRequest_ParticipantAdded()
    {
        // arrange

        // create table


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

        var innerRequest = new AddParticipantRequest
        {
            OrganizerEmail = newHat.Organizer.Email,
            HatId = newHat.Id,
            Name = "Joe Test",
            Email = "participant@test.com"
        };

        var request = new APIGatewayProxyRequest
        {
            Body = _jsonService.SerializeDefault(innerRequest)
        };

        var sut = new AddParticipant(_serviceProvider);

        // act
        var response = await sut.FunctionHandler(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }
}
