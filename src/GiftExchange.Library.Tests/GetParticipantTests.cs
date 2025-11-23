namespace GiftExchange.Library.Tests;

public class GetParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

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
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeDataProvider>());

        _sut = new GetParticipant(serviceProvider).FunctionHandler;
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
        var response = await _sut(new APIGatewayProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "showpickedrecipients", "true" },
                { "organizerEmail", hat.Organizer.Email },
                { "hatId", hat.Id.ToString() },
                { "participantEmail", person.Email }
            }
        }, _context);

        // assert
        response.StatusCode.Should().Be(200);
        var participant = _jsonService.DeserializeDefault<Participant>(response.Body);
        participant.Should().NotBeNull();
        participant.EligibleRecipients.Count.Should().Be(2);
    }
}
