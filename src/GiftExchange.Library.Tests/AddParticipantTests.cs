namespace GiftExchange.Library.Tests;

public class AddParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly IServiceProvider _serviceProvider;

    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    public AddParticipantTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        var dynamoDbClient = dbFixture.CreateClient();

        _context = new FakeLambdaContext();
        _jsonService = new JsonService();
        _serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        var dynamoDbService = _serviceProvider.GetRequiredService<DynamoDbService>();
        _testDataService = new TestDataService(dynamoDbService);
    }

    [Fact]
    public async Task AddParticipant_ValidRequest_CreatedResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var request = new APIGatewayProxyRequest
        {
            Body = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Joe Test",
                Email = "participant@test.com"
            })
        };

        var sut = new AddParticipant(_serviceProvider);

        // act
        var response = await sut.FunctionHandler(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddParticipant_SameEmailAttempt_ConflictResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var requestOne = new APIGatewayProxyRequest
        {
            Body = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Joe Test",
                Email = "participant@test.com"
            })
        };

        var requestTwo = new APIGatewayProxyRequest
        {
            Body = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Not Joe Test",
                Email = "participant@test.com"
            })
        };

        var sut = new AddParticipant(_serviceProvider);

        // act
        await sut.FunctionHandler(requestOne, _context);
        var response = await sut.FunctionHandler(requestTwo, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddParticipant_SameNameAttempt_ConflictResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var requestOne = new APIGatewayProxyRequest
        {
            Body = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Joe Test",
                Email = "participant@test.com"
            })
        };

        var requestTwo = new APIGatewayProxyRequest
        {
            Body = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Joe Test",
                Email = "joe@test.com"
            })
        };

        var sut = new AddParticipant(_serviceProvider);

        // act
        await sut.FunctionHandler(requestOne, _context);
        var response = await sut.FunctionHandler(requestTwo, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }
}
