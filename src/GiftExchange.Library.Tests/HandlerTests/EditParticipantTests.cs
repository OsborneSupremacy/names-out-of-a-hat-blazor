namespace GiftExchange.Library.Tests.HandlerTests;

public class EditParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly AddParticipantRequestFaker _addParticipantRequestFaker;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public EditParticipantTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        _addParticipantRequestFaker = new AddParticipantRequestFaker();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<GiftExchangeProvider>());

        _sut = new EditParticipant(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task EditParticipant_ValidPayload_OkResponse()
    {
        // arrange
        var participantNames = new List<string>();

        var hat = await _testDataService.CreateTestHatAsync();

        // add organizer as participant
        await _testDataService.CreateParticipantAsync(new AddParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Name = hat.Organizer.Name,
            Email = hat.Organizer.Email
        }, []);

        participantNames.Add(hat.Organizer.Name);

        // add other participants
        foreach(var otherParticipant in _addParticipantRequestFaker.Generate(5))
        {
            await _testDataService.CreateParticipantAsync(otherParticipant with
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id
            }, []);
            participantNames.Add(otherParticipant.Name);
        }

        // add participant to be edited
        var participantUt = _addParticipantRequestFaker.Generate() with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id
        };

        await _testDataService.CreateParticipantAsync(participantUt, []);

        var innerRequest = new EditParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Email = participantUt.Email,
            EligibleRecipients = participantNames.ToImmutableList()
        };

        var apiRequest = _jsonService
            .SerializeDefault(innerRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut(apiRequest, _context);

        var updatedParticipant = await _testDataService.GetParticipantAsync(
            hat.Organizer.Email,
            hat.Id,
            participantUt.Email
        );

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        updatedParticipant.EligibleRecipients.Should().BeEquivalentTo(participantNames);
    }
}
