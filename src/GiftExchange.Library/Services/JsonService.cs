using System.Text.Json;

namespace GiftExchange.Library.Services;

public static class JsonService
{
    private static JsonSerializerOptions GetDefaultSerializerOptions() =>
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    public static string SerializeDefault<T>(T value) =>
        JsonSerializer.Serialize(value, GetDefaultSerializerOptions());

    public static T? DeserializeDefault<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, GetDefaultSerializerOptions());
}
