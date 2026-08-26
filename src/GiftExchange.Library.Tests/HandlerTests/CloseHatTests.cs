namespace GiftExchange.Library.Tests.HandlerTests;

[Collection(PostgresCollection.Name)]
public class CloseHatTests
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly IApiGatewayHandler _sut;

    public CloseHatTests(PostgresFixture dbFixture)
    {
        DotEnv.Load();

        var contextFactory = dbFixture.CreateContextFactory();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(contextFactory)
            .AddSingleton<IContentModerationService, FakeContentModerationService>()
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _giftExchangeProvider = serviceProvider.GetRequiredService<GiftExchangeProvider>();
        _testDataService = new TestDataService(_giftExchangeProvider);
        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("post/hat/close");
    }

    [Fact]
    public async Task CloseHat_GivenCooledOffStatus_ReturnsOkResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        await _giftExchangeProvider
            .UpdateHatStatusAsync(hat.Organizer.Email, hat.Id, HatStatus.CooledOff);

        var innerRequest = new CloseHatRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        var updatedHat = await _testDataService
            .GetHatAsync(hat.Organizer.Email, hat.Id);

        updatedHat.Status.Should().Be(HatStatus.Closed);
    }

    [Fact]
    public async Task CloseHat_GivenInvitationsSentStatus_ReturnsConflictResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        await _giftExchangeProvider
            .UpdateHatStatusAsync(hat.Organizer.Email, hat.Id, HatStatus.InvitationsSent);

        var innerRequest = new CloseHatRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CloseHat_GivenInProgressStatus_ReturnsConflictResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var innerRequest = new CloseHatRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }
}
