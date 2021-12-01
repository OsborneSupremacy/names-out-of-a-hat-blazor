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

        public bool CheckVerified(Guid hatId, string organizerEmail)
        {
            if (!_memoryCache.TryGetValue(hatId, out OrganizerRegistration registrationOut))
                return false;

            // email address doesn't match
            return (registrationOut.OrganizerEmail.ContentEquals(organizerEmail)
                && registrationOut.Verified);
        }

        public bool CheckVerified(OrganizerRegistration registrationIn)
        {
            if (registrationIn is null) return false;

            if (!_memoryCache.TryGetValue(registrationIn.HatId, out OrganizerRegistration registrationOut))
                return false;

            // email address doesn't match
            return (registrationOut.OrganizerEmail.ContentEquals(registrationIn.OrganizerEmail)
                && registrationOut.Verified);
        }

        public bool Verify(OrganizerRegistration registrationIn)
        {
            if (registrationIn is null) return false;

            if (!_memoryCache.TryGetValue(registrationIn.HatId, out OrganizerRegistration registrationOut))
                return false;

            // email address doesn't match
            if(!registrationOut.OrganizerEmail.ContentEquals(registrationIn.OrganizerEmail))
                return false;

            // code doesn't match
            if (!registrationOut.VerificationCode.ContentEquals(registrationIn.VerificationCode))
                return false;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromDays(7));

            registrationOut.Verified = true;
            _memoryCache.Set(registrationIn.HatId, registrationOut, cacheEntryOptions);

            return true;
        }
    }
}
