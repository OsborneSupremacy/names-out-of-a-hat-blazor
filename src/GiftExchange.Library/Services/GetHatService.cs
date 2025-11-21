namespace GiftExchange.Library.Services;

public class GetHatService : IBusinessService<GetHatRequest, Hat>
{
    private readonly DynamoDbService _dynamoDbService;

    public GetHatService(DynamoDbService dynamoDbService)
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result<Hat>> ExecuteAsync(GetHatRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<Hat>(new KeyNotFoundException($"HatId {hat.Id} not found"), HttpStatusCode.NotFound);

        if(!request.ShowPickedRecipients)
            hat = RedactPickedRecipients(hat);

        return new Result<Hat>(hat, HttpStatusCode.OK);
    }

    private Hat RedactPickedRecipients(Hat hat) =>
        hat with
        {
            Participants = hat.Participants
                .Select(p => p with
                {
                    PickedRecipient = string.IsNullOrWhiteSpace(p.PickedRecipient) ? string.Empty : Persons.Redacted.Name
                })
                .ToImmutableList()
        };
}
