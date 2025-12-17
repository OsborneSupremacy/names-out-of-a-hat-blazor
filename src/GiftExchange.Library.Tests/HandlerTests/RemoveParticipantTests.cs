namespace GiftExchange.Library.Tests.HandlerTests;

public class RemoveParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly CreateHatRequestFaker _requestFaker;

    private readonly IApiGatewayHandler _sut;

    public RemoveParticipantTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        _requestFaker = new CreateHatRequestFaker();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());
        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("delete/participant");
    }

    [Fact]
    public async Task RemoveParticipant_GivenNonExistentHat_ReturnsNotFoundResponse()
    {
        // arrange
        var innerRequest = new RemoveParticipantRequest
        {
            OrganizerEmail = _requestFaker.Generate().OrganizerEmail,
            HatId = Guid.NewGuid(),
            Email = "nonexistent@example.com"
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveParticipant_GivenEmailSameAsOrganizer_ReturnsBadRequestResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var innerRequest = new RemoveParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Email = hat.Organizer.Email
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveParticipant_GivenNonExistentParticipant_ReturnsNoContentResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var innerRequest = new RemoveParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Email = "noone@here.example"
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveParticipant_GivenValidRequest_ReturnsNoContentResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var addFaker = new AddParticipantRequestFaker();

        var participant = addFaker.Generate() with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        };

        await _testDataService.CreateParticipantAsync(participant, []);

        var innerRequest = new RemoveParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Email = participant.Email
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.NoContent);

        var removed = await _testDataService
            .GetParticipantAsync(hat.Organizer.Email, hat.Id, participant.Email);

        removed.Should().Be(Participants.Empty);
    }
}
