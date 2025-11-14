namespace GiftExchange.Library.Services;

public class EditHatService : IServiceWithoutResponseBody<EditHatRequest>
{
    private readonly DynamoDbService _dynamoDbService;

    public EditHatService(DynamoDbService dynamoDbService)
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }
    
    public async Task<Result> ExecuteAsync(EditHatRequest request, ILambdaContext context)
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
