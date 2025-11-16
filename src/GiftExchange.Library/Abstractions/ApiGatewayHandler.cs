namespace GiftExchange.Library.Abstractions;

/// <summary>
/// An abstract base class that facilitates handling of API Gateway requests in a standardized way.
/// This class integrates with AWS Lambda and is designed to serve as a generic handler for API requests,
/// delegating the processing logic to a specified business service.
/// </summary>
/// <typeparam name="TRequest">The type of the request object expected by the business service.</typeparam>
/// <typeparam name="TService">The type of the business service that processes the request.</typeparam>
/// <typeparam name="TResponse">The type of the response object returned by the business service.</typeparam>
public abstract class ApiGatewayHandler<TRequest, TService, TResponse>
    where TRequest : class
    where TResponse : class
    where TService : IBusinessService<TRequest, TResponse>
{
    private IServiceProvider? _serviceProvider;
    private readonly object _serviceProviderLock = new();

    protected ApiGatewayHandler() { }

    protected ApiGatewayHandler(IServiceProvider serviceProvider)
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

    [UsedImplicitly]
    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var jsonService = GetServiceProvider()
            .GetService<JsonService>() ?? new JsonService();

        var innerRequest = GetRequestObject(request, jsonService);

        if(innerRequest.IsFaulted)
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)innerRequest.StatusCode,
                Headers = CorsHeaderService.GetCorsHeaders(),
                Body = innerRequest.Exception.Message
            };

        var service = GetServiceProvider().GetService<TService>();

        if (service is null)
        {
            context.Logger.LogCritical($"Could not resolve service of type {typeof(TService).FullName}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Headers = CorsHeaderService.GetCorsHeaders()
            };
        }

        var serviceResponse = await service
            .ExecuteAsync(innerRequest.Value, context)
            .ConfigureAwait(false);

        if(serviceResponse.IsFaulted)
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)serviceResponse.StatusCode,
                Headers = CorsHeaderService.GetCorsHeaders(),
                Body = serviceResponse.Exception.Message
            };

        if(this is IHasResponseBody<TResponse>)
            return new APIGatewayProxyResponse
            {
                StatusCode =  (int)serviceResponse.StatusCode,
                Headers = CorsHeaderService.GetCorsHeaders(),
                Body = jsonService.SerializeDefault(serviceResponse.Value)
            };

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)serviceResponse.StatusCode,
            Headers = CorsHeaderService.GetCorsHeaders()
        };
    }

    private Result<TRequest> GetRequestObject(APIGatewayProxyRequest request, JsonService jsonService) =>
        this switch
        {
            IHasRequestBody<TRequest> => GetRequestObjectFromBody(jsonService, request.Body),
            IHasRequestParameters<TRequest> hasRequestParameters => hasRequestParameters.Transform(request),
            _ => new Result<TRequest>(
                new AggregateException("Function is not configured to get request from either body or parameters."),
                HttpStatusCode.InternalServerError
            )
        };

    private Result<TRequest> GetRequestObjectFromBody(JsonService jsonService, string requestBody)
    {
        var innerRequest = jsonService.DeserializeDefault<TRequest>(requestBody);
        return innerRequest switch
        {
            null => new Result<TRequest>(
                new AggregateException("Request body could not be deserialized to configured request class."),
                HttpStatusCode.BadRequest
            ),
            _ => new Result<TRequest>(innerRequest, HttpStatusCode.OK)
        };
    }
}
