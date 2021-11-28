using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Server.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class OrganizerVerificationService
    {
        private readonly MemoryCache _memoryCache;

        public OrganizerVerificationService(MemoryCache memoryCache)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public bool VerifyAsync(Hat hat, string code)
        {
            if (_memoryCache.TryGetValue(hat.Id, out ICacheEntry value))
                return false;

            if (value is not OrganizerRegistration registration)
                return false;

            // email address doesn't match
            if(!registration.OrganizerEmail.ContentEquals(hat.Organizer?.Person.Email ?? string.Empty))
                return false;

            // code doesn't match
            if (!registration.VerificationCode.ContentEquals(code))
                return false;

            registration.Verified = true;
            value.SetValue(registration); 

            return true;
        }
    }
}
