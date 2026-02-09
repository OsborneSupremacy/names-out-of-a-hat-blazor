namespace GiftExchange.Library.Abstractions;

internal interface IContentModerationService
{
    public Task<(bool IsValid, string ErrorMessage)> ValidateContentAsync(string text, string fieldName);

    public Task<(bool IsValid, List<string> ErrorMessages)> ValidateMultipleFieldsAsync(
        Dictionary<string, string> fieldsToValidate);
}
