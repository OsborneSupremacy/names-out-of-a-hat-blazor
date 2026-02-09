namespace GiftExchange.Library.Tests.HandlerTests;

public class EditHatTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly IApiGatewayHandler _sut;

    public EditHatTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .AddSingleton<IContentModerationService, FakeContentModerationService>()
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());

        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("put/hat");
    }

    [Fact]
    public async Task EditHat_ValidRequest_OkResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var editHatRequest = new EditHatRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Name = "New Hat Name",
            AdditionalInformation = "New Additional Information",
            PriceRange = "$20 - $30"
        };

        var request = _jsonService
            .SerializeDefault(editHatRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(request, _context);
        var updatedHat = await _testDataService
            .GetHatAsync(editHatRequest.OrganizerEmail, hat.Id);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        updatedHat.Name.Should().Be(editHatRequest.Name);
        updatedHat.AdditionalInformation.Should().Be(editHatRequest.AdditionalInformation);
        updatedHat.PriceRange.Should().Be(editHatRequest.PriceRange);
    }
}
