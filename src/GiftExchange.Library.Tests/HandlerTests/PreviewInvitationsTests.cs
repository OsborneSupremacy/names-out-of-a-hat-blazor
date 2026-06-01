namespace GiftExchange.Library.Tests.HandlerTests;

public class PreviewInvitationsTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly IApiGatewayHandler _sut;

    public PreviewInvitationsTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddValidators()
            .AddSingleton(dynamoDbClient)
            .AddSingleton<IContentModerationService, FakeContentModerationService>()
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());
        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("get/hat/previewinvitations");
    }

    [Fact]
    public async Task PreviewInvitations_GivenValidPayload_ReturnsPreviewWithPlaceholders()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var apiRequest = new APIGatewayProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "hatId", hat.Id.ToString() },
                { "organizerEmail", hat.Organizer.Email }
            }
        };

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
            QueryStringParameters = new Dictionary<string, string>
            {
                { "hatId", Guid.NewGuid().ToString() },
                { "organizerEmail", "organizer@example.com" }
            }
        };

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
            QueryStringParameters = new Dictionary<string, string>
            {
                { "hatId", hat.Id.ToString() },
                { "organizerEmail", "not-an-email" }
            }
        };

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Body.ToLowerInvariant().Should().Contain("email");
    }
}
