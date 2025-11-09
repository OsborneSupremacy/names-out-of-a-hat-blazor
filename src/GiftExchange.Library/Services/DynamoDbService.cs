using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using dotenv.net.Utilities;

namespace GiftExchange.Library.Services;

internal class DynamoDbService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;

    private readonly string _tableName;

    public DynamoDbService(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
        _tableName = EnvReader.GetStringValue("TABLE_NAME");
    }

    public DynamoDbService()
    {
        _dynamoDbClient = new AmazonDynamoDBClient();
        _tableName = EnvReader.GetStringValue("TABLE_NAME");
    }

    public async Task<(bool exists, Guid hatId)> DoesHatExistAsync(string organizerEmail)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new()
            {
                ["PK"] = new AttributeValue { S = organizerEmail }
            }
        };

        var response = await _dynamoDbClient
            .GetItemAsync(request).ConfigureAwait(false);

        var exists = response.Item is { Count: > 0 };
        return (exists, exists ? Guid.Parse(response.Item["SK"].S) : Guid.Empty);
    }

    public async Task<Guid> CreateHatAsync(Hat hat)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = hat.Organizer.Person.Email },
            ["SK"] = new() { S = hat.Id.ToString() },
            ["HatId"] = new() { S = hat.Id.ToString() },
            ["Name"] = new() { S = hat.Name },
            ["AdditionalInformation"] = new() { S = hat.AdditionalInformation },
            ["PriceRange"] = new() { S = hat.PriceRange },
            ["OrganizerVerified"] = new() { BOOL = hat.OrganizerVerified },
            ["RecipientsAssigned"] = new() { BOOL = hat.RecipientsAssigned },
            ["Organizer"] = new() { S = JsonService.SerializeDefault(hat.Organizer) },
            ["Participants"] = new() { S = JsonService.SerializeDefault(hat.Participants) }
        };

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await _dynamoDbClient.PutItemAsync(request).ConfigureAwait(false);
        return hat.Id;
    }
}
