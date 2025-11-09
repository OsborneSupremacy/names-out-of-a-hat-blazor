namespace GiftExchange.Library.Handlers;

public class CreateHat
{
    public static async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await Handler.FunctionHandler<CreateHatRequest, CreateHatResponse>
        (
            request,
            InnerHandler,
            context
        );

    private static Task<Result<CreateHatResponse>> InnerHandler(CreateHatRequest request)
    {
        var response = new CreateHatResponse
        {
            HatId = Guid.NewGuid()
        };
        var result = new Result<CreateHatResponse>(response, HttpStatusCode.Created);
        return Task.FromResult(result);
    }
}
