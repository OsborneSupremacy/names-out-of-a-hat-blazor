using Amazon.DynamoDBv2.Model;
using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Providers;

[UsedImplicitly]
public class GiftExchangeProvider
{

    private readonly IAmazonDynamoDB _dynamoDbClient;

    private readonly string _tableName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GiftExchangeProvider(IAmazonDynamoDB dynamoDbClient)
    {
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="hatDataModel"></param>
    /// <returns>true if hat was created, false is had already existed.</returns>
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
            Item = item,
            ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
        };

        try
        {
            await _dynamoDbClient.PutItemAsync(request)
                .ConfigureAwait(false);
            return true;
        } catch (ConditionalCheckFailedException)
        {
            return false;
        }
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

        var participants = await GetParticipantsAsync(organizerEmail, hatId)
            .ConfigureAwait(false);

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
            Participants = participants
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

    public async Task<Participant> CreateParticipantAsync(
        AddParticipantRequest request,
        ImmutableList<Participant> existingParticipants
        )
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new() { S = $"ORGANIZER#{request.OrganizerEmail}#HAT#{request.HatId}#PARTICIPANT" },
            ["SK"] = new() { S = $"PARTICIPANT#{request.Email}" },
            ["Name"] = new() { S = request.Name },
            ["Email"] = new() { S = request.Email }
        };

        var eligibleParticipantNames = existingParticipants
            .Select(p => p.Person.Name)
            .ToList();

        if(existingParticipants.Any())
            item.Add("EligibleParticipants", new() { SS = eligibleParticipantNames });

        var putItemRequest = new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
            ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
        };

        await _dynamoDbClient.PutItemAsync(putItemRequest)
            .ConfigureAwait(false);

        return new Participant
        {
            PickedRecipient = string.Empty,
            Person = new Person
            {
                Name = request.Name,
                Email =  request.Email
            },
            EligibleRecipients = eligibleParticipantNames.ToImmutableList()
        };
    }

    public async Task<ImmutableList<Participant>> GetParticipantsAsync(
        string organizerEmail,
        Guid hatId
        )
    {
        var request = new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new() { S = $"ORGANIZER#{organizerEmail}#HAT#{hatId}#PARTICIPANT" },
                [":skPrefix"] = new() { S = "PARTICIPANT#" }
            },
            ConsistentRead = true
        };

        var response = await _dynamoDbClient
            .QueryAsync(request)
            .ConfigureAwait(false);

        if (response.Items is not { Count: > 0 })
            return [];

        return response.Items
            .Select(i => new Participant
            {
                PickedRecipient = i.TryGetValue("PickedRecipient", out var pr) ? pr.S : string.Empty,
                Person = new Person
                {
                    Email = i["Email"].S,
                    Name = i["Name"].S
                },
                EligibleRecipients = i.TryGetValue("EligibleParticipants", out var er)
                    ? er.SS.ToImmutableList()
                    : []
            })
            .ToImmutableList();
    }

    public async Task<(bool participantExists, Participant participant)> GetParticipantAsync(
        string requestOrganizerEmail,
        Guid requestHatId,
        string requestParticipantEmail
        )
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = $"ORGANIZER#{requestOrganizerEmail}#HAT#{requestHatId}#PARTICIPANT" },
                ["SK"] = new() { S = $"PARTICIPANT#{requestParticipantEmail}" }
            }
        };

        var response = await _dynamoDbClient
            .GetItemAsync(request)
            .ConfigureAwait(false);

        if (response.Item == null || response.Item.Count == 0)
            return (false, Participants.Empty);

        var participant = new Participant
        {
            PickedRecipient = response.Item.TryGetValue("PickedRecipient", out var pr) ? pr.S : string.Empty,
            Person = new Person
            {
                Email = response.Item["Email"].S,
                Name = response.Item["Name"].S
            },
            EligibleRecipients = response.Item.TryGetValue("EligibleParticipants", out var er)
                ? er.SS.ToImmutableList()
                : []
        };

        return (true, participant);
    }

    public async Task UpdateRecipientsAssignedAsync(
        string requestOrganizerEmail,
        Guid requestHatId,
        bool recipientsAssigned
        )
    {
        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = $"ORGANIZER#{requestOrganizerEmail}#HAT" },
                ["SK"] = new() { S = $"HAT#{requestHatId}" },
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

    public async Task AddParticipantEligibleRecipientAsync(
        string organizerEmail,
        Guid hatId,
        string participantEmail,
        string recipientName
        )
    {
        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = $"ORGANIZER#{organizerEmail}#HAT#{hatId}#PARTICIPANT" },
                ["SK"] = new() { S = $"PARTICIPANT#{participantEmail}" }
            },
            UpdateExpression = "ADD EligibleParticipants :newRecipient",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":newRecipient"] = new() { SS = [recipientName] }
            }
        };

        await _dynamoDbClient
            .UpdateItemAsync(updateRequest)
            .ConfigureAwait(false);
    }

    public async Task UpdateEligibleRecipientsAsync(
        string organizerEmail,
        Guid hatId,
        string participantEmail,
        ImmutableList<string> eligibleRecipients
        )
    {
        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = $"ORGANIZER#{organizerEmail}#HAT#{hatId}#PARTICIPANT" },
                ["SK"] = new() { S = $"PARTICIPANT#{participantEmail}" }
            },
            UpdateExpression = "SET EligibleParticipants = :eligibleRecipients",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":eligibleRecipients"] = new() { SS = eligibleRecipients.ToList() }
            }
        };

        await _dynamoDbClient
            .UpdateItemAsync(updateRequest)
            .ConfigureAwait(false);
    }

    public async Task DeleteParticipantAsync(string requestOrganizerEmail, Guid requestHatId, string requestEmail)
    {
        var deleteRequest = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = $"ORGANIZER#{requestOrganizerEmail}#HAT#{requestHatId}#PARTICIPANT" },
                ["SK"] = new() { S = $"PARTICIPANT#{requestEmail}" }
            }
        };

        await _dynamoDbClient
            .DeleteItemAsync(deleteRequest)
            .ConfigureAwait(false);
    }
}
