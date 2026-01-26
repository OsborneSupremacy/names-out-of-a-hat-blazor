namespace GiftExchange.Library.Extensions;

public static class ApiGatewayProxyRequestExtensions
{
    extension(APIGatewayProxyRequest request)
    {
        public Result<T> GetInnerRequest<T>(JsonService jsonService)
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

        public string GetEmailPathParameter()
        {
            return request.PathParameters.TryGetValue("email", out var email)
                ? System.Web.HttpUtility.UrlDecode(email)
                : string.Empty;
        }
    }
}
