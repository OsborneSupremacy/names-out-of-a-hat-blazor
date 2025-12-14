namespace GiftExchange.Library.Tests.HandlerTests;

public class AddParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly AddParticipantRequestFaker _requestFaker;

    private readonly IApiGatewayHandler _sut;

    public AddParticipantTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());

        _requestFaker = new AddParticipantRequestFaker();

        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("post/participant");
    }

    [Fact]
    public async Task AddParticipant_ValidRequest_CreatedResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var request = _jsonService.SerializeDefault(_requestFaker.Generate() with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        }).ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddParticipant_SameEmailAttempt_ConflictResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var innerRequest = _requestFaker.Generate();

        var requestOne =_jsonService.SerializeDefault(innerRequest with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        }).ToApiGatewayProxyRequest();

        var requestTwo = _jsonService.SerializeDefault(_requestFaker.Generate() with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Email = innerRequest.Email
        }).ToApiGatewayProxyRequest();

        // act
        await _sut.FunctionHandler(requestOne, _context);
        var response = await _sut.FunctionHandler(requestTwo, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddParticipant_SameNameAttempt_ConflictResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var innerRequest = _requestFaker.Generate();

        var requestOne = _jsonService.SerializeDefault(innerRequest with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        }).ToApiGatewayProxyRequest();

        var requestTwo = _jsonService.SerializeDefault(_requestFaker.Generate() with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Name = innerRequest.Name
        }).ToApiGatewayProxyRequest();

        // act
        await _sut.FunctionHandler(requestOne, _context);
        var response = await _sut.FunctionHandler(requestTwo, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }
}
