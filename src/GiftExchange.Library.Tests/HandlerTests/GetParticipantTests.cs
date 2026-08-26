namespace GiftExchange.Library.Tests.HandlerTests;

[Collection(PostgresCollection.Name)]
public class GetParticipantTests
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly IApiGatewayHandler _sut;

    public GetParticipantTests(PostgresFixture dbFixture)
    {
        DotEnv.Load();

        var contextFactory = dbFixture.CreateContextFactory();
        _context = new FakeLambdaContext();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(contextFactory)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());

        _sut = serviceProvider
            .GetRequiredKeyedService<IApiGatewayHandler>("get/participant/{organizeremail}/{hatid}/{participantemail}");
    }

    [Fact]
    public async Task GetParticipant_ValidRequest_ReturnsParticipantResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var personFaker = new PersonFaker();
        var person = personFaker.Generate();

        // Eligibility is stored by participant id now, so the people this participant may draw
        // have to be real rows rather than fakes.
        var existing = new List<Participant>();

        foreach (var other in personFaker.Generate(2))
            existing.Add(await _testDataService.CreateParticipantAsync(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = other.Name,
                Email = other.Email
            }, []));

        await _testDataService.CreateParticipantAsync(new AddParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Name = person.Name,
            Email = person.Email
        }, existing.ToImmutableList());

        // act
        var response = await _sut.FunctionHandler(new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string>
            {
                { "hatId", hat.Id.ToString() },
                { "participantEmail", person.Email }
            },
            QueryStringParameters = new Dictionary<string, string>()
        }.WithAuthenticatedEmail(hat.Organizer.Email), _context);

        // assert
        response.StatusCode.Should().Be(200);
        var participant = _jsonService.DeserializeDefault<Participant>(response.Body);
        participant.Should().NotBeNull();
        participant.EligibleRecipients.Count.Should().Be(2);
    }
}
