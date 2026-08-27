using Amazon.DynamoDBv2.Model;

namespace GiftExchange.Library.Providers;

/// <summary>
/// A per-address limit on how often this application will answer an unsolicited message.
/// </summary>
/// <remarks>
/// Automatic replies are the mechanism behind backscatter: something sends mail to a stranger
/// because a third party asked it to, and the stranger blames the sender. The inbound path guards
/// against that mainly by staying silent until it knows who wrote in, but the do-not-reply
/// auto-response cannot — its whole job is to answer somebody who is not a known participant.
///
/// Two things make it safe. The caller only answers a message SES has authenticated, so the From is
/// genuinely that domain's, and this caps how often any one address can be answered. Between them,
/// nobody can point this at a stranger and nobody can make it send twice.
///
/// The same conditional-put-with-TTL arrangement <c>LoginTokenProvider</c> uses to throttle magic
/// link requests, against the same table.
/// </remarks>
[UsedImplicitly]
internal class ReplyThrottleProvider : IReplyThrottleProvider
{
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromHours(24);

    private readonly IAmazonDynamoDB _dynamoDbClient;

    private readonly ILogger<ReplyThrottleProvider> _logger;

    private readonly string _tableName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReplyThrottleProvider(IAmazonDynamoDB dynamoDbClient, ILogger<ReplyThrottleProvider> logger)
    {
        _dynamoDbClient = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tableName = EnvReader.GetStringValue("TABLE_NAME");
    }

    /// <summary>
    /// Claims this address's one reply for the window.
    /// </summary>
    /// <returns>false when it has already been answered inside the window, and should not be again.</returns>
    public async Task<bool> TryReserveReplySlotAsync(string kind, string email)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiresAt = DateTimeOffset.UtcNow.Add(ThrottleWindow).ToUnixTimeSeconds();

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = $"REPLYTHROTTLE#{kind}#{email.TrimNullSafe().ToLowerInvariant()}" },
                ["SK"] = new() { S = "REPLYTHROTTLE" },
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
        catch (Exception exception)
        {
            // Fails closed, unlike most throttles. If the slot cannot be claimed, the reply is not
            // sent — an outage here must not turn into an unbounded run of automatic mail.
            _logger.LogError(exception, "Could not reserve a reply slot; suppressing the reply.");
            return false;
        }
    }
}
