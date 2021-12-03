﻿using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using System.Net.Http.Json;

namespace NamesOutOfAHat2.Client.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ClientHttpService
    {
        private readonly IHttpClientFactory _clientFactory;

        public ClientHttpService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public async Task<(bool success, string details)> VerifyAsync(NavigationManager navigationManager, OrganizerRegistration registration) =>
            await SendAsync(navigationManager, registration, "api/verify");

        public async Task<(bool success, string details)> CheckVerifiedAsync(NavigationManager navigationManager, OrganizerRegistration registration) =>
            await SendAsync(navigationManager, registration, "api/verify-check");

        public async Task<(bool success, string details)> SendVerificationAsync(NavigationManager navigationManager, Hat hat) =>
            await SendAsync(navigationManager, hat, "api/verify-register");

        public async Task<(bool success, string details)> SendAsync(NavigationManager navigationManager, Hat hat) => 
            await SendAsync(navigationManager, hat, "api/email");

        public async Task<(bool success, string details)> SendAsync<T>(NavigationManager navigationManager, T input, string relativeUrl)
        {
            var client = _clientFactory.CreateClient();

            using var response = await client.PostAsJsonAsync(
                navigationManager.ToAbsoluteUri(relativeUrl),
                input);

            return response.IsSuccessStatusCode switch
            {
                true => (true, string.Empty),
                _ => (false, await response.Content.ReadAsStringAsync())
            };
        }
    }
}