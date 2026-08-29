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

    /// <inheritdoc />
    public Task<ReserveSlotResponse> TryReserveAskSlotAsync(ReserveAskSlotRequest request) =>
        ReserveAsync(
            // Both ids, so that asking a second person is a separate slot from asking the first.
            $"ASKTHROTTLE#{request.AskerParticipantId}#{request.TargetParticipantId}",
            "ASKTHROTTLE",
            request.Window,
            "an Ask");

    /// <inheritdoc />
    public Task<ReserveSlotResponse> TryReserveAddressChangeSlotAsync(
        ReserveAddressChangeSlotRequest request
    ) =>
        ReserveAsync(
            $"ADDRESSCHANGETHROTTLE#{request.ParticipantId}",
            "ADDRESSCHANGETHROTTLE",
            request.Window,
            "an address change");

    /// <summary>
    /// The conditional put both slot methods are: write an item that expires, unless one is already
    /// there and has not.
    /// </summary>
    /// <remarks>
    /// One implementation rather than two, now that both speak the same request and answer with the
    /// same response. Everything that differed between them was the key, which is the only thing
    /// still passed in.
    /// </remarks>
    /// <param name="what">Names the thing being suppressed, for the log line when this fails.</param>
    private async Task<ReserveSlotResponse> ReserveAsync(
        string partitionKey,
        string sortKey,
        TimeSpan window,
        string what
    )
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(window).ToUnixTimeSeconds();

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new() { S = partitionKey },
                ["SK"] = new() { S = sortKey },
                // Still AskedAt, though this now also holds address changes. It is the attribute
                // name already written into live items, and renaming it would silently lose the
                // timestamp on every one still inside its window -- for no gain, since nothing
                // reads it but ReadAskedAt.
                ["AskedAt"] = new() { N = now.ToUnixTimeSeconds().ToString() },
                ["ExpiresAt"] = new() { N = expiresAt.ToString() },
                ["ttl"] = new() { N = expiresAt.ToString() }
            },
            ConditionExpression = "attribute_not_exists(PK) OR ExpiresAt < :now",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":now"] = new() { N = now.ToUnixTimeSeconds().ToString() }
            },
            // The item that blocked the write comes back with the rejection, which is the only way
            // to tell the caller when the last one was without a second read that could disagree
            // with the one that just refused them.
            ReturnValuesOnConditionCheckFailure = ReturnValuesOnConditionCheckFailure.ALL_OLD
        };

        try
        {
            await _dynamoDbClient.PutItemAsync(request).ConfigureAwait(false);
            return ReserveSlotResponses.Reserved;
        }
        catch (ConditionalCheckFailedException exception)
        {
            return ReserveSlotResponses.RefusedSince(ReadAskedAt(exception));
        }
        catch (Exception exception)
        {
            // Fails closed, as TryReserveReplySlotAsync does. An Ask not sent is a nuisance and an
            // organizer waiting a few minutes is an inconvenience; an outage that lifts either
            // limit is a way to send unmetered mail through this application.
            _logger.LogError(exception, "Could not reserve a slot; suppressing {What}.", what);
            return ReserveSlotResponses.Refused;
        }
    }

    /// <summary>
    /// When the Ask that blocked this one was made, or the minimum if the item did not come back.
    /// </summary>
    /// <remarks>
    /// Tolerant on purpose. The date only shapes a sentence in an email, so a missing or unparseable
    /// value is worth degrading over rather than failing over — the caller words it differently and
    /// the throttle still holds.
    /// </remarks>
    private static DateTimeOffset ReadAskedAt(ConditionalCheckFailedException exception) =>
        exception.Item is not null
        && exception.Item.TryGetValue("AskedAt", out var askedAt)
        && long.TryParse(askedAt.N, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : DateTimeOffset.MinValue;
}
