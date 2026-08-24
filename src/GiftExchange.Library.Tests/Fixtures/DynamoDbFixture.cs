using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Testcontainers.DynamoDb;

namespace GiftExchange.Library.Tests.Fixtures;

public class DynamoDbFixture : IAsyncLifetime
{
    /// <summary>
    /// Pinned explicitly: Testcontainers deprecated the implicit default image so that the tag a
    /// test run uses is visible in the repository rather than inherited from the package version.
    /// Tags: https://hub.docker.com/r/amazon/dynamodb-local/tags
    /// </summary>
    private const string DynamoDbImage = "amazon/dynamodb-local:1.21.0";

    private CancellationTokenSource CancellationTokenSource { get; }

    private readonly DynamoDbContainer _container;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public DynamoDbFixture()
    {
        DotEnv.Load();
        _container = new DynamoDbBuilder(DynamoDbImage).Build();
        CancellationTokenSource = new();
    }

    public IAmazonDynamoDB CreateClient()
    {
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = _container.GetConnectionString()
        };
        return new AmazonDynamoDBClient(new BasicAWSCredentials("test", "test"), config);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync(CancellationTokenSource.Token);
        await ProvisionTableAsync();
    }

    /// <summary>
    /// Provision table in the DynamoDB container that's equivalent to the production table that's provisioned via Terraform.
    /// </summary>
    private async Task ProvisionTableAsync()
    {
        using var client = CreateClient();

        var createRequest = new CreateTableRequest
        {
            TableName = EnvReader.GetStringValue("TABLE_NAME"),
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema =
            [
                new KeySchemaElement("PK", KeyType.HASH),
                new KeySchemaElement("SK", KeyType.RANGE)
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition("PK", ScalarAttributeType.S),
                new AttributeDefinition("SK", ScalarAttributeType.S)
            ]
        };

        await client.CreateTableAsync(createRequest, CancellationTokenSource.Token);
    }

    public async Task DisposeAsync()
    {
        await CancellationTokenSource.CancelAsync();
        await _container.DisposeAsync();
        CancellationTokenSource.Dispose();
    }
}
