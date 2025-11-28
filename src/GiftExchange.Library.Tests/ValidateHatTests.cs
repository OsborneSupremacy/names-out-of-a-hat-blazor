namespace GiftExchange.Library.Tests;

public class ValidateHatTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly AddParticipantRequestFaker _addParticipantRequestFaker;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public ValidateHatTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        _addParticipantRequestFaker = new AddParticipantRequestFaker();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());

        _sut = new ValidateHat(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task ValidateHat_GivenValidPayload_ReturnsOkResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var innerRequest = new ValidateHatRequest
        {
            HatId = hat.Id,
            OrganizerEmail = hat.Organizer.Email,
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }
}
