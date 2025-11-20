using Amazon.DynamoDBv2;
using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Tests;

public class GetHatsTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public GetHatsTests(DynamoDbFixture dbFixture)
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

        _sut = new GetHats(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task GetHats_ValidRequest_HatsReturned()
    {
        // arrange
        var hatOne = new HatDataModel
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

        var hatTwo = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            OrganizerName = "Barney Organizer",
            OrganizerEmail = "barney@test.org",
            HatName = "Test Hat Two",
            AdditionalInformation = "This is another test hat.",
            PriceRange = "$10 - $20",
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        await Task
            .WhenAll(_testDataService.CreateHatAsync(hatOne), _testDataService.CreateHatAsync(hatTwo));

        var request = new APIGatewayProxyRequest
        {
            PathParameters = new Dictionary<string, string> { ["email"] = "barney@test.org" }
        };

        // act
        var response = await _sut(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        var getHatsResponse = _jsonService.DeserializeDefault<GetHatsResponse>(response.Body);
        getHatsResponse!.Hats.Count.Should().Be(2);
    }
}
