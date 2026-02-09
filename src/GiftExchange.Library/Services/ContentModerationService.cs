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
    private const float ToxicityThreshold = 0.5f;

    public ContentModerationService(
        IAmazonComprehend comprehendClient,
        ILogger<ContentModerationService> logger)
    {
        _comprehendClient = comprehendClient ?? throw new ArgumentNullException(nameof(comprehendClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that the provided text does not contain toxic or inappropriate content
    /// </summary>
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
                .Where(label => label.Score >= ToxicityThreshold)
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
            _logger.LogError(ex, "Error during content moderation for {FieldName}", fieldName);
            return (true, string.Empty);
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
