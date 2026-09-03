using Amazon.Lambda.Serialization.SystemTextJson;
using GiftExchange.Library.Utility;

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

        // The two labels that make a trace findable. Annotations are indexed, so these turn "look
        // at a trace" into "look at every slow trace for this endpoint", and -- because a cold
        // start is the slow case and is rare by definition -- into being able to separate the two
        // populations rather than reading a p99 that mixes them.
        //
        // Safe to index: the resource is API Gateway's path template, so it carries {email} and
        // {id} as literal placeholders rather than anybody's address. Nothing identifying a person
        // is annotated anywhere; see the remarks on Tracing.Annotate for why that line is drawn
        // harder here than it is for logs.
        Tracing.Annotate("route", serviceKey);
        Tracing.Annotate("cold_start", isColdStart ? "true" : "false");

        if (service is not null)
            return await service.FunctionHandler(request, context);

        context.Logger.LogError($"Couldn't find api gateway handler for {serviceKey}");
        return ProxyResponseBuilder.Build(HttpStatusCode.InternalServerError);
    }
}
