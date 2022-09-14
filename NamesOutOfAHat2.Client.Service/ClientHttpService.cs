using LanguageExt.Common;

namespace NamesOutOfAHat2.Client.Service;

[ServiceLifetime(ServiceLifetime.Scoped)]
public class ClientHttpService
{
    private readonly IHttpClientFactory _clientFactory;

    public ClientHttpService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<Result<bool>> VerifyAsync(NavigationManager navigationManager, OrganizerRegistration registration) =>
        await SendAsync(navigationManager, registration, "api/verify");

    public async Task<Result<bool>> CheckVerifiedAsync(NavigationManager navigationManager, OrganizerRegistration registration) =>
        await SendAsync(navigationManager, registration, "api/verify-check");

    public async Task<Result<bool>> SendVerificationAsync(NavigationManager navigationManager, Hat hat) =>
        await SendAsync(navigationManager, hat, "api/verify-register");

    public async Task<Result<bool>> SendAsync(NavigationManager navigationManager, Hat hat) =>
        await SendAsync(navigationManager, hat, "api/email");

    public async Task<Result<bool>> SendAsync<T>(NavigationManager navigationManager, T input, string relativeUrl)
    {
        var client = _clientFactory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            navigationManager.ToAbsoluteUri(relativeUrl),
            input);

        var responseContent = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? true
            : new Result<bool>(new AggregateException(responseContent));
    }
}
