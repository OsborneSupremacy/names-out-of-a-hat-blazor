namespace GiftExchange.Library.Tests;

public class GetHatsTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly HatDataModelFaker _hatDataModelFaker;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

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

        _sut = new GetHats(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task GetHats_ValidRequest_HatsReturned()
    {
        // arrange
        var hatOne = _hatDataModelFaker.Generate();

        var hatTwo = _hatDataModelFaker.Generate() with
        {
            OrganizerEmail = hatOne.OrganizerEmail
        };

        await Task
            .WhenAll(_testDataService.CreateHatAsync(hatOne), _testDataService.CreateHatAsync(hatTwo));

        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string> { ["email"] = hatOne.OrganizerEmail }
        };

        // act
        var response = await _sut(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        var getHatsResponse = _jsonService.DeserializeDefault<GetHatsResponse>(response.Body);
        getHatsResponse!.Hats.Count.Should().Be(2);
    }
}
