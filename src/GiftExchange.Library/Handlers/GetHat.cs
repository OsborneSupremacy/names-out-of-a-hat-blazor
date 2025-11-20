namespace GiftExchange.Library.Handlers;

// ReSharper disable once UnusedType.Global
public class GetHat : ApiGatewayHandler<GetHatRequest, GetHatService, Hat>, IHasRequestParameters<GetHatRequest>, IHasResponseBody<Hat>
{
    public GetHat() { }

    public GetHat(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public Result<GetHatRequest> Transform(APIGatewayProxyRequest request)
    {
        var organizerEmail = request.QueryStringParameters["email"] ?? string.Empty;
        var hatId = Guid.TryParse(request.QueryStringParameters["id"], out var id) ? id : Guid.Empty;
        var showPickedRecipients = bool.TryParse(request.QueryStringParameters["showpickedrecipients"], out var boolOut) && boolOut;

        return new Result<GetHatRequest>(new GetHatRequest
        {
            OrganizerEmail = organizerEmail,
            HatId = hatId,
            ShowPickedRecipients = showPickedRecipients
        }, HttpStatusCode.OK);
    }
}
