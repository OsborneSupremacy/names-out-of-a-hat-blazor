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
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<DynamoDbService>());

        _sut = new GetParticipant(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task GetParticipant_ValidRequest_ReturnsParticipantResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var person = new Bogus.Faker().Person;

        await _testDataService.AddParticipantAsync(new AddParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Name = person.FirstName,
            Email = person.Email
        }, []);

        // act
        var response = await _sut(new APIGatewayProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "showpickedrecipients", "true" },
            },
            PathParameters = new Dictionary<string, string>
            {
                { "organizerEmail", hat.Organizer.Email },
                { "hatId", hat.Id.ToString() },
                { "participantEmail", person.Email }
            }
        }, _context);

        // assert
        response.StatusCode.Should().Be(200);
    }

}
