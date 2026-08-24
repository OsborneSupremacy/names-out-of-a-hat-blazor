namespace GiftExchange.Library.Tests.HandlerTests;

public class GetHatsTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly HatDataModelFaker _hatDataModelFaker;

    private readonly IApiGatewayHandler _sut;

    public GetHatsTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        _hatDataModelFaker = new HatDataModelFaker();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());

        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("get/hats/{email}");
    }

    [Fact]
    public async Task GetHats_ValidRequest_HatsReturned()
    {
        // arrange
        var hatOne = _hatDataModelFaker.Generate();

        var hatTwo = _hatDataModelFaker.Generate() with
        {
            OrganizerEmail = hatOne.OrganizerEmail,
            OrganizerName = hatOne.OrganizerName
        };

        await Task
            .WhenAll(_testDataService.CreateHatAsync(hatOne), _testDataService.CreateHatAsync(hatTwo));

        var request = new APIGatewayProxyRequest()
            .WithAuthenticatedEmail(hatOne.OrganizerEmail);

        // act
        var response = await _sut.FunctionHandler(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        var getHatsResponse = _jsonService.DeserializeDefault<GetHatsResponse>(response.Body);
        getHatsResponse!.Hats.Count.Should().Be(2);
        getHatsResponse.OrganizerName.Should().Be(hatOne.OrganizerName);
    }

    [Fact]
    public async Task GetHats_GivenOrganizerWithNoHats_ReturnsEmptyOrganizerName()
    {
        // arrange: nobody has created a hat for this address, so there is no name to read back and
        // the UI has to ask for one.
        var request = new APIGatewayProxyRequest()
            .WithAuthenticatedEmail("no.hats.yet@example.com");

        // act
        var response = await _sut.FunctionHandler(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        var getHatsResponse = _jsonService.DeserializeDefault<GetHatsResponse>(response.Body);
        getHatsResponse!.Hats.Should().BeEmpty();
        getHatsResponse.OrganizerName.Should().BeEmpty();
    }
}
