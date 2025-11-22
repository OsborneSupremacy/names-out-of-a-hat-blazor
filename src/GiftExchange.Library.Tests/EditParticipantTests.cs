namespace GiftExchange.Library.Tests;

public class EditParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly TestDataService _testDataService;

    private readonly Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> _sut;

    public EditParticipantTests(DynamoDbFixture dbFixture)
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

        _sut = new EditParticipant(serviceProvider).FunctionHandler;
    }

    [Fact]
    public async Task EditParticpant_ValidPayload_OkResponse()
    {
        // arrange



        // act


        // assert

    }
}
