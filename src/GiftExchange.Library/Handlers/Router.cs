namespace GiftExchange.Library.Handlers;

public class Router
{
    private IServiceProvider? _serviceProvider;
    private readonly object _serviceProviderLock = new();

    public Router() { }

    protected Router(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private IServiceProvider GetServiceProvider()
    {
        if(_serviceProvider is not null) return _serviceProvider;
        lock (_serviceProviderLock)
        {
            if(_serviceProvider is not null) return _serviceProvider;
            _serviceProvider = ServiceProviderBuilder.Build();
        }
        return _serviceProvider;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var serviceKey = $"{request.HttpMethod}/{request.Resource}".ToLowerInvariant();
        var service = GetServiceProvider()
            .GetKeyedService<IApiGatewayHandler>(serviceKey);

        if (service is not null)
            return await service.FunctionHandler(request, context);

        context.Logger.LogError($"Couldn't find api gateway handler for {serviceKey}");
        return ProxyResponseBuilder.Build(HttpStatusCode.InternalServerError);
    }
}
