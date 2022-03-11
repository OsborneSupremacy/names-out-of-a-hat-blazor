using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Utility;
using blazored = Blazored.LocalStorage;

namespace NamesOutOfAHat2.Client.Service
{
    [RegistrationTarget(typeof(Interface.ILocalStorageService))]
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class LocalStorageService : Interface.ILocalStorageService
    {
        private readonly blazored.ILocalStorageService _storage;

        public LocalStorageService(blazored.ILocalStorageService storage)
        {
            _storage = storage;
        }

        public async Task<string> GetFromLocalStorage(string key) =>
            await _storage.GetItemAsStringAsync(key);

        public async Task SetLocalStorage(string key, string value) =>
            await _storage.SetItemAsStringAsync(key, value);
    }
}