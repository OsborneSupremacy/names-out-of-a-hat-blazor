namespace GiftExchange.Library.Handlers;

public class EditHat
{
    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public EditHat()
    {
        _dynamoDbService = new DynamoDbService();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler.FunctionHandler<EditHatRequest>
        (
            request,
            InnerHandler,
            context
        );

    private async Task<Result> InnerHandler(EditHatRequest request)
    {
        var (hatExists, _ ) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        await _dynamoDbService.EditHatAsync(request)
            .ConfigureAwait(false);

        return new Result(HttpStatusCode.OK);
    }
}
