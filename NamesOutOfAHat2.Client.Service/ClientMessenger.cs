using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using System.Net.Http.Json;

namespace NamesOutOfAHat2.Client.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ClientMessenger
    {
        private readonly IHttpClientFactory _clientFactory;

        public ClientMessenger(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public async Task<(bool success, string details)> SendVerificationAsync(NavigationManager navigationManager, Hat hat) =>
            await SendAsync(navigationManager, hat, "api/verify");

        public async Task<(bool success, string details)> SendAsync(NavigationManager navigationManager, Hat hat) => 
            await SendAsync(navigationManager, hat, "api/email");

        public async Task<(bool success, string details)> SendAsync<T>(NavigationManager navigationManager, T input, string relativeUrl)
        {
            var client = _clientFactory.CreateClient();

            using var response = await client.PostAsJsonAsync(
                navigationManager.ToAbsoluteUri(relativeUrl),
                input);

            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var details = await response.Content.ReadAsStringAsync();

            return (false, details);
        }
    }
}
