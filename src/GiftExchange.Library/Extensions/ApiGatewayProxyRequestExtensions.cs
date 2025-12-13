namespace GiftExchange.Library.Extensions;

public static class ApiGatewayProxyRequestExtensions
{
    public static Result<T> GetInnerRequest<T>(this APIGatewayProxyRequest request, JsonService jsonService)
    {
        var innerRequest = jsonService.DeserializeDefault<T>(request.Body);
        return innerRequest switch
        {
            null => new Result<T>(
                new AggregateException("Request body could not be deserialized to configured request class."),
                HttpStatusCode.BadRequest
            ),
            _ => new Result<T>(innerRequest, HttpStatusCode.OK)
        };
    }
}
