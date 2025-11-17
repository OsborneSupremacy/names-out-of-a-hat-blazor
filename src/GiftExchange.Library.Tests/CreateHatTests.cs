namespace GiftExchange.Library.Tests;

public class CreateHatTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public CreateHatTests(DynamoDbFixture dbFixture)
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
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<DynamoDbService>());

        _sut = new CreateHat(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task CreateHat_ValidRequest_CreatedResponse()
    {
        // arrange
        var request = _jsonService.SerializeDefault(new CreateHatRequest
        {
            HatName = "2025 Gift Exchange",
            OrganizerName = "Alice Organizer",
            OrganizerEmail = "alice@test.com"
        }).ToApiGatewayProxyRequest();

        // act
        var response = await _sut(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateHat_HatAlreadyExists_OkResponse()
    {
        // arrange
        var request = _jsonService.SerializeDefault(new CreateHatRequest
        {
            HatName = "2026 Gift Exchange",
            OrganizerName = "Malice Organizer",
            OrganizerEmail = "malice@test.com"
        }).ToApiGatewayProxyRequest();

        // act
        await _sut(request, _context);
        var response = await _sut(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }
}
