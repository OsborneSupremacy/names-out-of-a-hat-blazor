using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Tests;

public class GetHatTests: IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public GetHatTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        var dynamoDbClient = dbFixture.CreateClient();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _testDataService = new TestDataService(serviceProvider.GetRequiredService<DynamoDbService>());

        _sut = new GetHat(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task GetHat_ValidRequest_HatReturned()
    {
        // arrange
        var hat = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            OrganizerName = "Barney Organizer",
            OrganizerEmail = "barney@test.org",
            HatName = "Test Hat One",
            AdditionalInformation = "This is a test hat.",
            PriceRange = "$10 - $20",
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        await _testDataService.CreateHatAsync(hat);
        await _testDataService.AddParticipantAsync(new AddParticipantRequest
        {
            OrganizerEmail = hat.OrganizerEmail,
            HatId = hat.HatId,
            Name = hat.OrganizerName,
            Email = hat.OrganizerEmail
        }, []);

        var getHatRequest = new GetHatRequest
        {
            OrganizerEmail = hat.OrganizerEmail,
            HatId = hat.HatId,
            ShowPickedRecipients = false
        };

        var request = new APIGatewayProxyRequest
        {
            QueryStringParameters = new Dictionary<string, string>
            {
                { "email", getHatRequest.OrganizerEmail },
                { "id", getHatRequest.HatId.ToString() },
                { "showpickedrecipients", getHatRequest.ShowPickedRecipients.ToString() }
            }
        };

        // act
        var response = await _sut(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        var getHatResponse = _jsonService.DeserializeDefault<Hat>(response.Body);
        getHatResponse!.Id.Should().Be(hat.HatId);
        getHatResponse.Participants.Should().HaveCount(1);
    }
}
