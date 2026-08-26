namespace GiftExchange.Library.Services;

/// <summary>
/// Service for moderating user-generated content using AWS Comprehend
/// </summary>
[UsedImplicitly]
internal class ContentModerationService : IContentModerationService
{
    private readonly IAmazonComprehend _comprehendClient;

    private readonly ILogger<ContentModerationService> _logger;

    // Threshold for toxicity detection (0.0 to 1.0)
    // 0.5 is recommended by AWS for balanced detection
    private readonly float _toxicityThreshold;

    public ContentModerationService(
        IAmazonComprehend comprehendClient,
        ILogger<ContentModerationService> logger)
    {
        _comprehendClient = comprehendClient ?? throw new ArgumentNullException(nameof(comprehendClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toxicityThreshold = (float)EnvReader.GetDoubleValue("CONTENT_MODERATION_THRESHOLD");
    }

    /// <summary>
    /// Validates that the provided text does not contain toxic or inappropriate content.
    /// </summary>
    /// <remarks>
    /// Fails closed: if the check cannot be performed, the content is rejected rather than
    /// accepted. Empty text is the one exception, since there is nothing to check.
    /// </remarks>
    /// <param name="text">The text to validate</param>
    /// <param name="fieldName">The name of the field being validated (for error messages)</param>
    /// <returns>A tuple indicating if validation passed and an error message if it failed</returns>
    public async Task<(bool IsValid, string ErrorMessage)> ValidateContentAsync(string text, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (true, string.Empty);

        try
        {
            var request = new DetectToxicContentRequest
            {
                LanguageCode = LanguageCode.En,
                TextSegments = [new() { Text = text }]
            };

            var response = await _comprehendClient.DetectToxicContentAsync(request);

            if (response.ResultList is null || response.ResultList.Count == 0)
                return (true, string.Empty);

            var result = response.ResultList[0];
            var toxicLabels = result.Labels
                .Where(label => label.Score >= _toxicityThreshold)
                .ToList();

            if (toxicLabels.Count <= 0)
                return (true, string.Empty);

            var labelNames = string.Join(", ", toxicLabels.Select(l => l.Name));
            _logger.LogWarning(
                "Content moderation flagged {FieldName} with toxic content. Labels: {Labels}, Scores: {Scores}",
                fieldName,
                labelNames,
                string.Join(", ", toxicLabels.Select(l => l.Score))
            );

            return (false, $"The {fieldName} contains inappropriate content and cannot be accepted.");

        }
        catch (Exception ex)
        {
            // Fail closed. Everything moderated here ends up in an email sent from our SES
            // identity, so accepting unchecked content during a Comprehend outage would put that
            // reputation in someone else's hands. The AWS SDK has already exhausted its own
            // retries by the time an exception reaches us, so there is nothing left to wait for.
            _logger.LogError(ex, "Content moderation failed for {FieldName}; rejecting the request.", fieldName);

            // Deliberately distinct from the rejection message below: the caller's content may be
            // perfectly fine, and telling someone their name is inappropriate when the checker was
            // simply unreachable is both wrong and unhelpful.
            return (false, $"We couldn't check the {fieldName} just now. Please try again in a moment.");
        }
    }

    /// <summary>
    /// Validates multiple text fields at once
    /// </summary>
    /// <param name="fieldsToValidate">Dictionary of field names to their text values</param>
    /// <returns>A tuple indicating if all validations passed and a list of error messages</returns>
    public async Task<(bool IsValid, List<string> ErrorMessages)> ValidateMultipleFieldsAsync(
        Dictionary<string, string> fieldsToValidate)
    {
        var errorMessages = new List<string>();

        foreach (var (fieldName, text) in fieldsToValidate)
        {
            var (isValid, errorMessage) = await ValidateContentAsync(text, fieldName);
            if (!isValid && errorMessage.Any())
                errorMessages.Add(errorMessage);
        }

        return (errorMessages.Count == 0, errorMessages);
    }
}
