using GiftExchange.Library.Tests.Extensions;

namespace GiftExchange.Library.Tests;

public class AddParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public AddParticipantTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        var dynamoDbService = serviceProvider.GetRequiredService<DynamoDbService>();
        _jsonService = serviceProvider.GetRequiredService<JsonService>();

        _testDataService = new TestDataService(dynamoDbService);

        _sut = new AddParticipant(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task AddParticipant_ValidRequest_CreatedResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var request = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Joe Test",
                Email = "participant@test.com"
            }).ToApiGatewayProxyRequest();

        // act
        var response = await _sut(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddParticipant_SameEmailAttempt_ConflictResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var requestOne =_jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Joe Test",
                Email = "participant@test.com"
            }).ToApiGatewayProxyRequest();

        var requestTwo = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Not Joe Test",
                Email = "participant@test.com"
            }).ToApiGatewayProxyRequest();

        // act
        await _sut(requestOne, _context);
        var response = await _sut(requestTwo, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddParticipant_SameNameAttempt_ConflictResponse()
    {
        // arrange
        var hat = await _testDataService.CreateTestHatAsync();

        var requestOne = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Joe Test",
                Email = "participant@test.com"
            }).ToApiGatewayProxyRequest();


        var requestTwo = _jsonService.SerializeDefault(new AddParticipantRequest
            {
                OrganizerEmail = hat.Organizer.Email,
                HatId = hat.Id,
                Name = "Joe Test",
                Email = "joe@test.com"
            }).ToApiGatewayProxyRequest();

        // act
        await _sut(requestOne, _context);
        var response = await _sut(requestTwo, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
    }
}
