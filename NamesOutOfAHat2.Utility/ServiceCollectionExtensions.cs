using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace NamesOutOfAHat2.Utility;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterServicesInAssembly(this IServiceCollection serviceCollection, Type type)
    {
        var services = Assembly.GetAssembly(type)!
            .GetTypes()
            .Where(x => !x.IsAbstract)
            .Where(x => x.IsPublic)
            .ToList();

        foreach (var service in services)
        {
            var lifetime = service.GetCustomAttribute<ServiceLifetimeAttribute>()?.ServiceLifetime;
            if (lifetime == null)
                continue;

            var targetType = service.GetCustomAttribute<RegistrationTargetAttribute>()?.Type;

            if (targetType == null)
                serviceCollection.AddService(service, lifetime.Value);
            else
                serviceCollection.AddService(targetType, service, lifetime.Value);
        }

        return serviceCollection;
    }

    public static IServiceCollection AddService(
        this IServiceCollection serviceCollection,
        Type service,
        ServiceLifetime serviceLifetime)
    {

        Func<Type, IServiceCollection> addDelegate = serviceLifetime switch
        {
            ServiceLifetime.Transient => (Type service) => { return serviceCollection.AddTransient(service); }
            ,
            ServiceLifetime.Singleton => (Type service) => { return serviceCollection.AddSingleton(service); }
            ,
            ServiceLifetime.Scoped => (Type service) => { return serviceCollection.AddScoped(service); }
            ,
            _ => throw new NotSupportedException()
        };
        return addDelegate(service);
    }

    public static IServiceCollection AddService(
        this IServiceCollection serviceCollection,
        Type serviceType,
        Type implementationType,
        ServiceLifetime serviceLifetime)
    {

        Func<Type, Type, IServiceCollection> addDelegate = serviceLifetime switch
        {
            ServiceLifetime.Transient => (Type serviceType, Type implementationType) => { return serviceCollection.AddTransient(serviceType, implementationType); }
            ,
            ServiceLifetime.Singleton => (Type serviceType, Type implementationType) => { return serviceCollection.AddSingleton(serviceType, implementationType); }
            ,
            ServiceLifetime.Scoped => (Type serviceType, Type implementationType) => { return serviceCollection.AddScoped(serviceType, implementationType); }
            ,
            _ => throw new NotSupportedException()
        };
        return addDelegate(serviceType, implementationType);
    }
}
