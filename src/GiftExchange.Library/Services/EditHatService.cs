namespace GiftExchange.Library.Services;

public class EditHatService : IBusinessService<EditHatRequest, StatusCodeOnlyResponse>
{
    private readonly DynamoDbService _dynamoDbService;

    public EditHatService(DynamoDbService dynamoDbService)
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(EditHatRequest request, ILambdaContext context)
    {
        var (hatExists, _ ) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(
                new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound
            );

        await _dynamoDbService.EditHatAsync(request)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
