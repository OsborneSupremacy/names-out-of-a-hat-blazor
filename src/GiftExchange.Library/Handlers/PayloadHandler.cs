namespace GiftExchange.Library.Handlers;

/// <summary>
/// A generic payload handler for AWS Lambda functions using API Gateway.
/// It deserializes the incoming request, invokes the provided inner handler,
/// and constructs the appropriate API Gateway response based on the result.
/// </summary>
internal static class PayloadHandler
{
    public static async Task<APIGatewayProxyResponse> FunctionHandler<TRequest, TResponse>(
        APIGatewayProxyRequest request,
        Func<TRequest, Task<Result<TResponse>>> innerHandler,
        ILambdaContext context
    )
    {
        var innerRequest = JsonService.DeserializeDefault<TRequest>(request.Body);
        if (innerRequest is null)
            return ApiGatewayProxyResponses.BadRequest;

        var innerResponse = await innerHandler(innerRequest);

        if(innerResponse.IsSuccess)
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)innerResponse.StatusCode,
                Headers = CorsHeaderService.GetCorsHeaders(),
                Body = JsonService.SerializeDefault(innerResponse.Value)
            };

        return new APIGatewayProxyResponse
        {
            StatusCode =  (int)innerResponse.StatusCode,
            Headers = CorsHeaderService.GetCorsHeaders(),
            Body = JsonService.SerializeDefault(innerResponse.Exception.Message)
        };
    }

    public static async Task<APIGatewayProxyResponse> FunctionHandler<TRequest>(
        APIGatewayProxyRequest request,
        Func<TRequest, Task<Result>> innerHandler,
        ILambdaContext context
    )
    {
        var innerRequest = JsonService.DeserializeDefault<TRequest>(request.Body);
        if (innerRequest is null)
            return ApiGatewayProxyResponses.BadRequest;

        var innerResponse = await innerHandler(innerRequest);

        if(innerResponse.IsSuccess)
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)innerResponse.StatusCode,
                Headers = CorsHeaderService.GetCorsHeaders()
            };

        return new APIGatewayProxyResponse
        {
            StatusCode =  (int)innerResponse.StatusCode,
            Headers = CorsHeaderService.GetCorsHeaders(),
            Body = JsonService.SerializeDefault(innerResponse.Exception.Message)
        };
    }
}
