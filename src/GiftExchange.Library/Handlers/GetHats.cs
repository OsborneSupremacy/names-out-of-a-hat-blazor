namespace GiftExchange.Library.Handlers;

// ReSharper disable once UnusedType.Global
public class GetHats : ApiGatewayHandler<GetHatsRequest, GetHatsService, GetHatsResponse>, IHasRequestParameters<GetHatsRequest>, IHasResponseBody<GetHatsResponse>
{
    public Result<GetHatsRequest> Transform(APIGatewayProxyRequest request)
    {
        var organizerEmail = request.PathParameters["email"] ?? string.Empty;
        return new Result<GetHatsRequest>(new GetHatsRequest
        {
            OrganizerEmail = organizerEmail
        }, HttpStatusCode.OK);
    }
}
