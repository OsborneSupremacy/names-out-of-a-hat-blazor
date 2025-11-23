using System.Net.Mime;

namespace GiftExchange.Library.Providers;

internal static class CorsHeaderProvider
{
   public static Dictionary<string, string> GetCorsHeaders() =>
       new()
       {
           { "Content-Type", MediaTypeNames.Application.Json },
           { "Access-Control-Allow-Origin", "*" }
       };
}
