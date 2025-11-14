namespace GiftExchange.Library.Abstractions;

public abstract class HandlerWithoutResponseBody<TRequest, TService> : HandlerBase where TService : IServiceWithoutResponseBody<TRequest>
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
                Headers = CorsHeaderService.GetCorsHeaders()
            };

        return new APIGatewayProxyResponse
        {
            StatusCode =  (int)serviceResponse.StatusCode,
            Headers = CorsHeaderService.GetCorsHeaders(),
            Body = jsonService.SerializeDefault(serviceResponse.Exception.Message)
        };
    }
}
