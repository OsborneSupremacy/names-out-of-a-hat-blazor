using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Tracing;
using AWS.Lambda.Powertools.Metrics;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<GiftExchangeJsonSerializerContext>))]

namespace GiftExchange.Library.Handlers;

[UsedImplicitly]
public class Router
{
    private IServiceProvider? _serviceProvider;
    private readonly Lock _serviceProviderLock = new();

    public Router() { }

    protected Router(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private IServiceProvider GetServiceProvider()
    {
        if(_serviceProvider is not null) return _serviceProvider;
        using (_serviceProviderLock.EnterScope())
        {
            if(_serviceProvider is not null) return _serviceProvider;
            _serviceProvider = ServiceProviderBuilder.Build();
        }
        return _serviceProvider;
    }

    [Metrics(CaptureColdStart = true, Namespace = nameof(Router))]
    [Tracing]
    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var serviceKey = $"{request.HttpMethod}{request.Resource}".ToLowerInvariant();

        // Read before the provider is built, because building it is what stops it being one.
        var isColdStart = _serviceProvider is null;

        var service = GetServiceProvider()
            .GetKeyedService<IApiGatewayHandler>(serviceKey);

        Tracing.AddAnnotation("route", serviceKey);

        if (service is not null)
            return await service.FunctionHandler(request, context);

        context.Logger.LogError($"Couldn't find api gateway handler for {serviceKey}");
        return ProxyResponseBuilder.Build(HttpStatusCode.InternalServerError);
    }
}
