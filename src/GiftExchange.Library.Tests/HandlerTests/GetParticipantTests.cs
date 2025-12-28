namespace GiftExchange.Library.Tests.HandlerTests;

public class GetParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly IApiGatewayHandler _sut;

    public GetParticipantTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());

        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("get/participant/{organizeremail}/{hatid}/{participantemail}");
    }

    [Fact]
    public async Task GetParticipant_ValidRequest_ReturnsParticipantResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var personFaker = new PersonFaker();
        var participantFaker = new ParticipantFaker();

        var person = personFaker.Generate();

        await _testDataService.CreateParticipantAsync(new AddParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Name = person.Name,
            Email = person.Email
        }, participantFaker.Generate(2).ToImmutableList());

        // act
        var response = await _sut.FunctionHandler(new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "organizerEmail", hat.Organizer.Email },
                { "hatId", hat.Id.ToString() },
                { "participantEmail", person.Email }
            },
            QueryStringParameters = new Dictionary<string, string>
            {
                { "showpickedrecipients", "true" }
            }
        }, _context);

        // assert
        response.StatusCode.Should().Be(200);
        var participant = _jsonService.DeserializeDefault<Participant>(response.Body);
        participant.Should().NotBeNull();
        participant.EligibleRecipients.Count.Should().Be(2);
    }
}
