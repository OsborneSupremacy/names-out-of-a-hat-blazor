namespace GiftExchange.Library.Tests.HandlerTests;

public class CreateHatTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly CreateHatRequestFaker _requestFaker;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public CreateHatTests(DynamoDbFixture dbFixture)
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
        _sut = new CreateHat(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task CreateHat_ValidRequest_CreatedResponse()
    {
        // arrange
        var request = _jsonService
            .SerializeDefault(_requestFaker.Generate())
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateHat_HatAlreadyExists_OkResponse()
    {
        // arrange
        var request = _jsonService
            .SerializeDefault(_requestFaker.Generate())
            .ToApiGatewayProxyRequest();

        // act
        _ = await _sut(request, _context);
        var response = await _sut(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }
}
