namespace GiftExchange.Library.Abstractions;

public abstract class HandlerBase
{
    private IServiceProvider? _serviceProvider;
    private readonly object _serviceProviderLock = new();

    protected IServiceProvider GetServiceProvider()
    {
        if(_serviceProvider is not null) return _serviceProvider;
        lock (_serviceProviderLock)
        {
            if(_serviceProvider is not null) return _serviceProvider;
            _serviceProvider = ServiceProviderBuilder.Build();
        }
        return _serviceProvider;
    }
}
