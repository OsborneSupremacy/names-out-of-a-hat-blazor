namespace GiftExchange.Library.Handlers;

// ReSharper disable once UnusedType.Global
public class GetHats : ApiGatewayHandler<GetHatsRequest, GetHatsService, GetHatsResponse>, IHasRequestParameters<GetHatsRequest>, IHasResponseBody<GetHatsResponse>
{
    public GetHats() { }

    public GetHats(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public Result<GetHatsRequest> Transform(APIGatewayProxyRequest request)
    {
        var organizerEmail = request.PathParameters.TryGetValue("email", out var email) ? email : string.Empty;
        return new Result<GetHatsRequest>(new GetHatsRequest
        {
            OrganizerEmail = organizerEmail
        }, HttpStatusCode.OK);
    }
}
