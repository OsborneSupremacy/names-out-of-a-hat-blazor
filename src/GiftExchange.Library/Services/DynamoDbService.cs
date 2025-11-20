using Amazon.DynamoDBv2.Model;
using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Services;

[UsedImplicitly]
public class DynamoDbService
{
    private readonly JsonService _jsonService;

    private readonly IAmazonDynamoDB _dynamoDbClient;

    private readonly string _tableName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DynamoDbService(IAmazonDynamoDB dynamoDbClient, JsonService jsonService)
    {
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
        _dynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
        _tableName = EnvReader.GetStringValue("TABLE_NAME");
    }

    public async Task<ImmutableList<HatMetaData>> GetHatsAsync(string organizerEmail)
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new() { S = $"ORGANIZER#{organizerEmail}#HAT" },
                [":skPrefix"] = new() { S = "HAT#" }
            },
            ConsistentRead = true
        };

        var response = await _dynamoDbClient.QueryAsync(request)
            .ConfigureAwait(false);

        if (response.Items is not { Count: > 0 })
            return [];

        return response.Items
            .Select(i => new HatMetaData
            {
                HatId = Guid.Parse(i["HatId"].S),
                HatName = i["HatName"].S
            })
            .ToImmutableList();
    }

    public async Task<bool> CreateHatAsync(HatDataModel hatDataModel)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = $"ORGANIZER#{hatDataModel.OrganizerEmail}#HAT" },
            ["SK"] = new() { S = $"HAT#{hatDataModel.HatId}" },
            ["HatId"] = new() { S = hatDataModel.HatId.ToString() },
            ["OrganizerName"] = new() { S = hatDataModel.OrganizerName },
            ["HatName"] = new() { S = hatDataModel.HatName },
            ["AdditionalInformation"] = new() { S = hatDataModel.AdditionalInformation },
            ["PriceRange"] = new() { S = hatDataModel.PriceRange },
            ["OrganizerVerified"] = new() { BOOL = hatDataModel.OrganizerVerified },
            ["RecipientsAssigned"] = new() { BOOL = hatDataModel.RecipientsAssigned }
        };

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item
        };

        await _dynamoDbClient.PutItemAsync(request)
            .ConfigureAwait(false);

        return true;
    }

    public async Task<(bool exists, Guid hatId)> DoesHatAlreadyExistAsync(
        string organizerEmail,
        string hatName
    )
    {
        var hats = await GetHatsAsync(organizerEmail)
            .ConfigureAwait(false);

        if(!hats.Any())
            return (false, Guid.Empty);

        return hats.FirstOrDefault(h => h.HatName.ContentEquals(hatName)) is { } hat
            ? (true, hat.HatId)
            : (false, Guid.Empty);
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
                ["PK"] = new() { S = $"ORGANIZER#{organizerEmail}#HAT" },
                ["SK"] = new() { S = $"HAT#{hatId}" }
            }
        };

        var response = await _dynamoDbClient
            .GetItemAsync(request)
            .ConfigureAwait(false);

        if (response.Item == null || response.Item.Count == 0)
            return (false, Hats.Empty);

        var hat = new Hat
        {
            Id = Guid.Parse(response.Item["HatId"].S), Name = response.Item["HatName"].S,
            AdditionalInformation = response.Item["AdditionalInformation"].S,
            PriceRange = response.Item["PriceRange"].S,
            OrganizerVerified = response.Item["OrganizerVerified"].BOOL ?? false,
            RecipientsAssigned = response.Item["RecipientsAssigned"].BOOL ?? false,
            Organizer = new Person {
                Name = response.Item["OrganizerName"].S,
                Email = organizerEmail
            },
            Participants = []
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
                ["PK"] = new() { S = $"ORGANIZER#{request.OrganizerEmail}#HAT" },
                ["SK"] = new() { S = $"HAT#{request.HatId}" }
            },
            UpdateExpression = "SET HatName = :name, AdditionalInformation = :additionalInfo, PriceRange = :priceRange",
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
                [":participants"] = new() { S = _jsonService.SerializeDefault(newParticipants) },
                [":recipientsAssigned"] = new() { BOOL = false }
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
