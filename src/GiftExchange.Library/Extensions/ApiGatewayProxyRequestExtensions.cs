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

        public string GetEmailPathParameter() =>
            request.PathParameters.TryGetValue("email", out var email)
                ? System.Web.HttpUtility.UrlDecode(email)
                : string.Empty;

        public Guid GetIdPathParameter()
        {
            string id = request.PathParameters.TryGetValue("id", out var idOut) ? idOut! : string.Empty;
            return string.ToGuidOrEmpty(id);
        }

        /// <summary>
        /// The caller's email as established by the authorizer, not by anything the client sent.
        /// Empty when the request did not pass through the authorizer.
        /// </summary>
        public string GetAuthenticatedEmail() =>
            request.RequestContext?.Authorizer is { } authorizer
            && authorizer.TryGetValue("email", out var authorizedEmail)
                ? authorizedEmail?.ToString() ?? string.Empty
                : string.Empty;

        public Guid GetHatIdPathParameter()
        {
            string id = request.PathParameters.TryGetValue("hatId", out var idOut) ? idOut! : string.Empty;
            return string.ToGuidOrEmpty(id);
        }
    }
}
