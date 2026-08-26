namespace GiftExchange.Library.Tests.HandlerTests;

/// <summary>
/// The organizer email travels in request bodies and always has, but it is no longer trusted:
/// the adapter overwrites it with whatever the authorizer established. These tests pin that down,
/// because the failure mode is silent — everything still returns a plausible status code.
/// </summary>
[Collection(PostgresCollection.Name)]
public class AuthorizationTests
{
    private const string OtherUserEmail = "someone.else@example.com";

    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly IApiGatewayHandler _deleteHat;

    private readonly IApiGatewayHandler _getHat;

    public AuthorizationTests(PostgresFixture dbFixture)
    {
        DotEnv.Load();

        var contextFactory = dbFixture.CreateContextFactory();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddValidators()
            .AddSingleton(contextFactory)
            .AddSingleton<IContentModerationService, FakeContentModerationService>()
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());
        _deleteHat = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("delete/hat");
        _getHat = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("get/hat/{email}/{id}");
    }

    [Fact]
    public async Task DeleteHat_GivenBodyNamingAnotherOrganizer_LeavesThatOrganizersHatIntact()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var innerRequest = new DeleteHatRequest
        {
            OrganizerEmail = hat.Organizer.Email, // the body asks for the victim's hat...
            HatId = hat.Id
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest(OtherUserEmail); // ...but somebody else is signed in

        // act
        await _deleteHat.FunctionHandler(apiRequest, _context);

        // assert
        var survivingHat = await _testDataService.GetHatAsync(hat.Organizer.Email, hat.Id);
        survivingHat.Id.Should().Be(hat.Id);
    }

    [Fact]
    public async Task GetHat_GivenAnotherOrganizersHatId_ReturnsNotFound()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var apiRequest = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "id", hat.Id.ToString() }
            },
            QueryStringParameters = new Dictionary<string, string>()
        }.WithAuthenticatedEmail(OtherUserEmail);

        // act
        var response = await _getHat.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHat_GivenOwnHatId_ReturnsTheHat()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var apiRequest = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "id", hat.Id.ToString() }
            },
            QueryStringParameters = new Dictionary<string, string>()
        }.WithAuthenticatedEmail(hat.Organizer.Email);

        // act
        var response = await _getHat.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        _jsonService.DeserializeDefault<Hat>(response.Body)!.Id.Should().Be(hat.Id);
    }
}
