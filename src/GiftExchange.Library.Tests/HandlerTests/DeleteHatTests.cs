namespace GiftExchange.Library.Tests.HandlerTests;

public class DeleteHatTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly AddParticipantRequestFaker _participantFaker;

    private readonly IApiGatewayHandler _sut;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public DeleteHatTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        _participantFaker = new AddParticipantRequestFaker();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());
        _giftExchangeProvider = serviceProvider.GetRequiredService<GiftExchangeProvider>();

        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("delete/hat");
    }

    [Fact]
    public async Task DeleteHat_GivenValidRequest_ReturnsNoContentAndRemovesHat()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var participantRequest = _participantFaker.Generate() with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        };

        await _testDataService.CreateParticipantAsync(participantRequest, []);

        var deleteRequest = new DeleteHatRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        };

        var apiRequest = _jsonService
            .SerializeDefault(deleteRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut.FunctionHandler(apiRequest, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.NoContent);

        var (exists, _) = await _giftExchangeProvider.GetHatAsync(hat.Organizer.Email, hat.Id);
        exists.Should().BeFalse();
    }
}
