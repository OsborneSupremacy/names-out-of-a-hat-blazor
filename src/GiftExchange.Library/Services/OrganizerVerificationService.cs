namespace GiftExchange.Library.Services;

internal class OrganizerVerificationService : IApiGatewayHandler
{
    private readonly ApiGatewayAdapter _adapter;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public OrganizerVerificationService(
        ApiGatewayAdapter adapter,
        GiftExchangeProvider giftExchangeProvider
        )
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        => _adapter.AdaptAsync<VerifyOrganizerRequest, StatusCodeOnlyResponse>(request, VerifyOrganizerAsync);

    internal async Task<Result<StatusCodeOnlyResponse>> VerifyOrganizerAsync(
        VerifyOrganizerRequest request
        )
    {
        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists) // we do not reveal whether the hat exists for security reasons
            return new Result<StatusCodeOnlyResponse>(new UnauthorizedAccessException("Invalid verification code"), HttpStatusCode.BadRequest);

        if(!int.TryParse(request.VerificationCode.TrimNullSafe(), out var verificationCode))
            return new Result<StatusCodeOnlyResponse>(new UnauthorizedAccessException("Invalid verification code"), HttpStatusCode.Unauthorized);

        var isValid = await _giftExchangeProvider
            .VerifyVerificationCodeAsync(request.OrganizerEmail, request.HatId, verificationCode.ToString())
            .ConfigureAwait(false);

        if(!isValid)
            return new Result<StatusCodeOnlyResponse>(new UnauthorizedAccessException("Invalid verification code"), HttpStatusCode.Unauthorized);

        await _giftExchangeProvider
            .MarkOrganizerVerifiedAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }

}
