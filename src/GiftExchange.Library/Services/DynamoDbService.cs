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
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new() { S = organizerEmail }
            },
            ProjectionExpression = "SK",
            Limit = 1
        };

        var response = await _dynamoDbClient.QueryAsync(request).ConfigureAwait(false);

        if (response.Items is { Count: > 0 } &&
            response.Items[0].TryGetValue("SK", out var skAttr))
            return (true, Guid.Parse(skAttr.S));

        return (false, Guid.Empty);
    }

    public async Task<Guid> CreateHatAsync(Hat hat)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = hat.Organizer.Email },
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

    public async Task<(bool exists, Hat hat)> GetHatAsync(string organizerEmail, Guid hatId)
    {
        if (string.IsNullOrWhiteSpace(organizerEmail) || hatId == Guid.Empty)
            return (false, Hats.Empty);

        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = organizerEmail },
                ["SK"] = new() { S = hatId.ToString() }
            }
        };

        var response = await _dynamoDbClient.GetItemAsync(request).ConfigureAwait(false);

        if (response.Item == null || response.Item.Count == 0)
            return (false, Hats.Empty);

        var hat = new Hat
        {
            Id = Guid.Parse(response.Item["HatId"].S),
            Name = response.Item["Name"].S,
            AdditionalInformation = response.Item["AdditionalInformation"].S,
            PriceRange = response.Item["PriceRange"].S,
            OrganizerVerified = response.Item["OrganizerVerified"].BOOL ?? false,
            RecipientsAssigned = response.Item["RecipientsAssigned"].BOOL ?? false,
            Organizer = JsonService.DeserializeDefault<Person>(response.Item["Organizer"].S) ?? Persons.Empty,
            Participants = JsonService.DeserializeDefault<ImmutableList<Participant>>(response.Item["Participants"].S) ?? [ ]
        };
        return (true, hat);
    }

    public async Task EditHatAsync(EditHatRequest request)
    {
        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = request.OrganizerEmail },
                ["SK"] = new() { S = request.HatId.ToString() }
            },
            UpdateExpression = "SET #name = :name, AdditionalInformation = :additionalInfo, PriceRange = :priceRange",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#name"] = "Name"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":name"] = new() { S = request.Name },
                [":additionalInfo"] = new() { S = request.AdditionalInformation },
                [":priceRange"] = new() { S = request.PriceRange }
            }
        };

        await _dynamoDbClient.UpdateItemAsync(updateRequest).ConfigureAwait(false);
    }

    public async Task UpdateParticipantsAsync(
        string requestOrganizerEmail,
        Guid requestHatId,
        ImmutableList<Participant> newParticipants
        )
    {
        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = requestOrganizerEmail },
                ["SK"] = new() { S = requestHatId.ToString() }
            },
            UpdateExpression = "SET Participants = :participants, RecipientsAssigned = :recipientsAssigned",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":participants"] = new() { S = JsonService.SerializeDefault(newParticipants) },
                [":recipientsAssigned"] = new() { BOOL = false }
            }
        };

        await _dynamoDbClient
            .UpdateItemAsync(updateRequest)
            .ConfigureAwait(false);
    }

    public async Task UpdateOrganizerNameAsync(string requestOrganizerEmail, Guid requestHatId, string requestName)
    {
        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = requestOrganizerEmail },
                ["SK"] = new() { S = requestHatId.ToString() }
            },
            UpdateExpression = "SET #name = :name",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#name"] = "Organizer.Person.Name"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":name"] = new() { S = requestName }
            }
        };

        await _dynamoDbClient
            .UpdateItemAsync(updateRequest)
            .ConfigureAwait(false);
    }

    public async Task UpdateRecipientsAssignedAsync(string requestOrganizerEmail, Guid requestHatId, bool recipientsAssigned)
    {
        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = requestOrganizerEmail },
                ["SK"] = new() { S = requestHatId.ToString() }
            },
            UpdateExpression = "SET RecipientsAssigned = :recipientsAssigned",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":recipientsAssigned"] = new() { BOOL = recipientsAssigned }
            }
        };

        await _dynamoDbClient
            .UpdateItemAsync(updateRequest)
            .ConfigureAwait(false);
    }
}
