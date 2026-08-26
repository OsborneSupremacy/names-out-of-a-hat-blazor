
namespace GiftExchange.Library.Tests.HandlerTests;

[Collection(PostgresCollection.Name)]
public class CreateHatTests
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly CreateHatRequestFaker _requestFaker;

    private readonly IApiGatewayHandler _sut;

    public CreateHatTests(PostgresFixture dbFixture)
    {
        DotEnv.Load();

        _requestFaker = new CreateHatRequestFaker();

        var contextFactory = dbFixture.CreateContextFactory();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(contextFactory)
            .AddSingleton<IContentModerationService, FakeContentModerationService>()
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("post/hat");
    }

    [Fact]
    public async Task CreateHat_ValidRequest_CreatedResponse()
    {
        // arrange
        var request = _jsonService
            .SerializeDefault(_requestFaker.Generate())
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateHat_HatAlreadyExists_ConflictResponse()
    {
        // arrange
        var request = _jsonService
            .SerializeDefault(_requestFaker.Generate())
            .ToApiGatewayProxyRequest();

        // act
        _ = await _sut.FunctionHandler(request, _context);
        var response = await _sut.FunctionHandler(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }
}
