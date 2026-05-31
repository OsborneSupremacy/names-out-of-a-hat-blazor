using System.Text.Json;

namespace GiftExchange.Library.Services;

public class JsonService
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JsonService(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
    }

    public string SerializeDefault<T>(T value) =>
        JsonSerializer.Serialize(value, _jsonSerializerOptions);

    public T? DeserializeDefault<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, _jsonSerializerOptions);
}
