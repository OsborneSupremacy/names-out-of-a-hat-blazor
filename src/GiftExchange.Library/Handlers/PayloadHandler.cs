namespace GiftExchange.Library.Handlers;

internal static class PayloadHandler
{
    public static async Task<APIGatewayProxyResponse> FunctionHandler<TRequest, TResponse>(
        APIGatewayProxyRequest request,
        Func<TRequest, Task<Result<TResponse>>> innerHandler,
        ILambdaContext context
    )
    {
        var innerRequest = JsonService.DeserializeDefault<TRequest>(request.Body);
        if (innerRequest == null)
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
            StatusCode = (int)HttpStatusCode.BadRequest,
            Headers = CorsHeaderService.GetCorsHeaders(),
            Body = JsonService.SerializeDefault(innerResponse.Exception.Message)
        };
    }
}
