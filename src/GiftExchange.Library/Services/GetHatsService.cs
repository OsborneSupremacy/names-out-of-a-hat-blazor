namespace GiftExchange.Library.Services;

public class GetHatsService : IBusinessService<GetHatsRequest, GetHatsResponse>
{
    private readonly DynamoDbService _dynamoDbService;

    public GetHatsService(DynamoDbService dynamoDbService)
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result<GetHatsResponse>> ExecuteAsync(GetHatsRequest request, ILambdaContext context)
    {
        var result = await _dynamoDbService
            .GetHatsAsync(request.OrganizerEmail)
            .ConfigureAwait(false);

        return new Result<GetHatsResponse>(new GetHatsResponse
        {
            Hats = result
        }, HttpStatusCode.OK);
    }
}
