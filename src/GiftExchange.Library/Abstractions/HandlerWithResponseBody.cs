namespace GiftExchange.Library.Abstractions;

public abstract class HandlerWithResponseBody<TRequest, TService, TResponse> : HandlerBase where TService : IServiceWithResponseBody<TRequest, TResponse>
{
    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var jsonService = GetServiceProvider().GetService<JsonService>()!;

        var innerRequest = jsonService.DeserializeDefault<TRequest>(request.Body);
        if (innerRequest is null)
            return ApiGatewayProxyResponses.BadRequest;

        var service = GetServiceProvider().GetService<TService>()!;
        var serviceResponse = await service.ExecuteAsync(innerRequest, context);

        if(serviceResponse.IsSuccess)
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)serviceResponse.StatusCode,
                Headers = CorsHeaderService.GetCorsHeaders(),
                Body = jsonService.SerializeDefault(serviceResponse.Value)
            };

        return new APIGatewayProxyResponse
        {
            StatusCode =  (int)serviceResponse.StatusCode,
            Headers = CorsHeaderService.GetCorsHeaders(),
            Body = jsonService.SerializeDefault(serviceResponse.Exception.Message)
        };
    }
}
