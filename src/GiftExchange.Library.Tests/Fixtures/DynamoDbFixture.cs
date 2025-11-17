using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Testcontainers.DynamoDb;

namespace GiftExchange.Library.Tests.Fixtures;

public class DynamoDbFixture : IAsyncLifetime
{
    private CancellationTokenSource CancellationTokenSource { get; }

    private readonly DynamoDbContainer _container;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public DynamoDbFixture()
    {
        DotEnv.Load();
        _container = new DynamoDbBuilder().Build();
        CancellationTokenSource = new();
    }

    public IAmazonDynamoDB CreateClient()
    {
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = _container.GetConnectionString()
        };

        var credentials = new BasicAWSCredentials("test", "test");
        return new AmazonDynamoDBClient(credentials, config);
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
