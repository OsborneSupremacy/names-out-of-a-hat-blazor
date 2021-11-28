using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Server.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class OrganizerRegistrationService
    {
        private readonly MemoryCache _memoryCache;

        public OrganizerRegistrationService(MemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public void Register(Hat hat, string code)
        {
            if (hat.Id == Guid.Empty)
                throw new ArgumentException("Hat ID is invalid");

            // remove existing entry, if found
            if (_memoryCache.TryGetValue(hat.Id, out _))
                _memoryCache.Remove(hat.Id);

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromDays(7));

            OrganizerRegistration registration = new()
            {
                HatId = hat.Id,
                OrganizerEmail = hat.Organizer?.Person.Email ?? string.Empty,
                VerificationCode = code,
                Verified = false
            };

            _memoryCache.Set(hat.Id, registration, cacheEntryOptions);
        }
    }
}
