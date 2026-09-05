
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

    /// <summary>
    /// The allowance is per organizer, so a whole day's worth from one address spends it and the
    /// next one has to be refused. Everything up to the limit is asserted too: a limit that also
    /// refused the fifth would pass a test that only looked at the sixth.
    /// </summary>
    [Fact]
    public async Task CreateHat_DailyLimitSpent_TooManyRequestsResponse()
    {
        // arrange
        var organizer = _requestFaker.Generate();

        for (var created = 0; created < HatCreationLimiter.DailyLimit; created++)
        {
            var allowed = await _sut.FunctionHandler(AnotherHatFor(organizer), _context);
            allowed.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        // act
        var response = await _sut.FunctionHandler(AnotherHatFor(organizer), _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// The organizer's next exchange: a freshly faked one carrying their name and address, so it
    /// differs from the last only in the ways that do not matter to the limit.
    /// </summary>
    private APIGatewayProxyRequest AnotherHatFor(CreateHatRequest organizer) =>
        _jsonService
            .SerializeDefault(_requestFaker.Generate() with
            {
                OrganizerName = organizer.OrganizerName,
                OrganizerEmail = organizer.OrganizerEmail
            })
            .ToApiGatewayProxyRequest();
}
