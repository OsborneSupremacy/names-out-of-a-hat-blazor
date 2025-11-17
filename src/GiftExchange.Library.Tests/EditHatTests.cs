namespace GiftExchange.Library.Tests;

public class EditHatTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public EditHatTests(DynamoDbFixture dbFixture)
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

        _sut = new EditHat(serviceProvider).FunctionHandler;
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

        var request = _jsonService.SerializeDefault(editHatRequest)
            .ToApiGatewayProxyRequest();

        // act
        var response = await _sut(request, _context);
        var updatedHat = await _testDataService.GetHatAsync(editHatRequest.OrganizerEmail, hat.Id);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        updatedHat.Name.Should().Be(editHatRequest.Name);
        updatedHat.AdditionalInformation.Should().Be(editHatRequest.AdditionalInformation);
        updatedHat.PriceRange.Should().Be(editHatRequest.PriceRange);
    }
}
