using Amazon.DynamoDBv2.Model;

namespace GiftExchange.Library.Providers;

/// <summary>
/// Storage for single-use magic link tokens. Only the hash of a token is ever persisted, so a
/// dump of the table does not let an attacker redeem pending links.
/// </summary>
[UsedImplicitly]
internal class LoginTokenProvider
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(1);

    private readonly IAmazonDynamoDB _dynamoDbClient;

    private readonly string _tableName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public LoginTokenProvider(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
        _tableName = EnvReader.GetStringValue("TABLE_NAME");
    }

    /// <summary>
    /// Issues a login token for the supplied address, storing only its hash.
    /// </summary>
    /// <returns>The plaintext token. This is the only time it exists outside the email.</returns>
    public async Task<string> CreateLoginTokenAsync(string email)
    {
        var token = SecretToken.Create(SecretToken.OpaqueTokenBytes);
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = BuildLoginKey(token) },
                ["SK"] = new() { S = "LOGIN" },
                ["Email"] = new() { S = NormalizeEmail(email) },
                ["ExpiresAt"] = new() { N = expiresAt.ToUnixTimeSeconds().ToString() },
                ["ttl"] = new() { N = expiresAt.ToUnixTimeSeconds().ToString() }
            }
        };

        await _dynamoDbClient.PutItemAsync(request).ConfigureAwait(false);

        return token;
    }

    /// <summary>
    /// Atomically consumes a login token. The conditional delete is what makes a token single-use:
    /// two concurrent redemptions cannot both succeed.
    /// </summary>
    public async Task<(bool redeemed, string email)> TryRedeemLoginTokenAsync(string token)
    {
        var request = new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = BuildLoginKey(token) },
                ["SK"] = new() { S = "LOGIN" }
            },
            ConditionExpression = "attribute_exists(PK)",
            ReturnValues = ReturnValue.ALL_OLD
        };

        try
        {
            var response = await _dynamoDbClient
                .DeleteItemAsync(request)
                .ConfigureAwait(false);

            // DynamoDB deletes expired items on its own schedule (typically within 48 hours), so an
            // item being present is not proof that it is still live. Check the expiry ourselves.
            if (!response.Attributes.TryGetValue("ExpiresAt", out var expiresAt)
                || long.Parse(expiresAt.N) < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return (false, string.Empty);

            return (true, response.Attributes["Email"].S);
        }
        catch (ConditionalCheckFailedException)
        {
            // Unknown, already redeemed, or reaped by TTL.
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Per-address throttle for link requests. Without this the endpoint is an open email relay
    /// pointed at arbitrary addresses, which is a deliverability and billing problem before it is
    /// a security one.
    /// </summary>
    /// <returns>false when a link was already requested for this address inside the window.</returns>
    public async Task<bool> TryReserveRequestSlotAsync(string email)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiresAt = DateTimeOffset.UtcNow.Add(ThrottleWindow).ToUnixTimeSeconds();

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = $"LOGINTHROTTLE#{NormalizeEmail(email)}" },
                ["SK"] = new() { S = "LOGINTHROTTLE" },
                ["ExpiresAt"] = new() { N = expiresAt.ToString() },
                ["ttl"] = new() { N = expiresAt.ToString() }
            },
            ConditionExpression = "attribute_not_exists(PK) OR ExpiresAt < :now",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":now"] = new() { N = now.ToString() }
            }
        };

        try
        {
            await _dynamoDbClient.PutItemAsync(request).ConfigureAwait(false);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    /// <summary>
    /// The item key a token is stored under: its digest, never the token.
    /// </summary>
    /// <remarks>
    /// The prefix is what keeps login items apart from the throttle items sharing this table. The
    /// hashing itself is <see cref="SecretToken"/>'s, so this class and the gift ideas routing
    /// tokens cannot drift into two different ideas of what "stored as a hash" means.
    /// </remarks>
    private static string BuildLoginKey(string token) =>
        $"LOGIN#{SecretToken.Hash(token)}";

    private static string NormalizeEmail(string email) =>
        email.TrimNullSafe().ToLowerInvariant();
}
