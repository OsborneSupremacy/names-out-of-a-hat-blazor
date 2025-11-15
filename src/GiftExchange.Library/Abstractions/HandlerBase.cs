namespace GiftExchange.Library.Abstractions;

/// <summary>
///
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TService">The service that will in invoked</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract class HandlerBase<TRequest, TService, TResponse>
    where TRequest : class
    where TResponse : class
    where TService : IBusinessService<TRequest, TResponse>
{
    private IServiceProvider? _serviceProvider;
    private readonly object _serviceProviderLock = new();

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
        var jsonService = GetServiceProvider().GetService<JsonService>()!;

        var innerRequest = GetRequestObject(request, jsonService);

        if(innerRequest.IsFaulted)
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)innerRequest.StatusCode,
                Headers = CorsHeaderService.GetCorsHeaders(),
                Body = innerRequest.Exception.Message
            };

        var service = GetServiceProvider().GetService<TService>()!;
        var serviceResponse = await service.ExecuteAsync(innerRequest.Value, context);

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

    private Result<TRequest> GetRequestObject(APIGatewayProxyRequest request, JsonService jsonService)
    {
        if (this is IHasRequestBody<TRequest>)
            return GetRequestObjectFromBody(jsonService, request.Body);
        if (this is IHasRequestParameters<TRequest> hasRequestParameters)
            return hasRequestParameters.Transform(request);
        return new Result<TRequest>(new AggregateException("Function is not configured to get request from either body or parameters."), HttpStatusCode.InternalServerError);
    }

    private Result<TRequest> GetRequestObjectFromBody(JsonService jsonService, string requestBody)
    {
        var innerRequest = jsonService.DeserializeDefault<TRequest>(requestBody);
        if (innerRequest is null)
            return new Result<TRequest>(new AggregateException("Request body could not be deserialized to configured request class."), HttpStatusCode.BadRequest);
        return new Result<TRequest>(innerRequest, HttpStatusCode.OK);
    }
}
