using Microsoft.Extensions.DependencyInjection;

namespace NamesOutOfAHat2.Utility
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ServiceLifetimeAttribute : Attribute
    {
        public ServiceLifetimeAttribute(ServiceLifetime serviceLifetime)
        {
            ServiceLifetime = serviceLifetime;
        }

        public ServiceLifetime ServiceLifetime { get; }
    }
}