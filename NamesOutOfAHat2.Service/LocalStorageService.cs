using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
    [RegistrationTarget(typeof(ILocalStorageService))]
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _js;

        private const string _identifier = "localStorage.getItem";

        public LocalStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string> GetFromLocalStorage(string key)
        {
            return await _js.InvokeAsync<string>(_identifier, key);
        }

        public async Task SetLocalStorage(string key, string value)
        {
            await _js.InvokeVoidAsync(_identifier, key, value);
        }
    }
}