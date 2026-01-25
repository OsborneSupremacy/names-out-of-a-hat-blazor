using System.Net.Mime;

namespace GiftExchange.Library.Providers;

internal static class CorsHeaderProvider
{
   public static Dictionary<string, string> GetCorsHeaders() =>
       new()
       {
           { "Content-Type", MediaTypeNames.Application.Json },
           { "Access-Control-Allow-Origin", "*" },
           { "Access-Control-Allow-Headers", "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token" },
           { "Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS" }
       };
}
