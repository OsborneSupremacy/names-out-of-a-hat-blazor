namespace GiftExchange.Library.Tests.HandlerTests;

[Collection(PostgresCollection.Name)]
public class PreviewInvitationsTests
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly IApiGatewayHandler _sut;

    public PreviewInvitationsTests(PostgresFixture dbFixture)
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
        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("get/hat/{email}/previewinvitations/{id}");
    }

    [Fact]
    public async Task PreviewInvitations_GivenValidPayload_ReturnsPreviewWithPlaceholders()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var apiRequest = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "id", hat.Id.ToString() }
            }
        }.WithAuthenticatedEmail(hat.Organizer.Email);

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        var preview = _jsonService.DeserializeDefault<PreviewInvitationsResponse>(response.Body);
        preview.Should().NotBeNull();
        preview!.Subject.Should().Contain(hat.Organizer.Name);
        preview.HtmlBody.Should().Contain("[Participant Name]");
        preview.HtmlBody.Should().Contain("[Picked Name]");
    }

    [Fact]
    public async Task PreviewInvitations_GivenMissingHat_ReturnsNotFound()
    {
        // arrange
        var apiRequest = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "id", Guid.NewGuid().ToString() }
            }
        }.WithAuthenticatedEmail("organizer@example.com");

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PreviewInvitations_GivenInvalidEmail_ReturnsBadRequest()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var apiRequest = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "id", hat.Id.ToString() }
            }
        }.WithAuthenticatedEmail("not-an-email");

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Body.ToLowerInvariant().Should().Contain("email");
    }
}
