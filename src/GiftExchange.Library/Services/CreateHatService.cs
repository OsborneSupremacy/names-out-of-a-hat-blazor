using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Services;

public class CreateHatService : IBusinessService<CreateHatRequest, CreateHatResponse>
{
    private readonly DynamoDbService _dynamoDbService;

    public CreateHatService(DynamoDbService dynamoDbService)
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result<CreateHatResponse>> ExecuteAsync(CreateHatRequest request, ILambdaContext context)
    {
        var (hatExists, hatId) = await _dynamoDbService
            .DoesHatAlreadyExistAsync(request.OrganizerEmail, request.HatName)
            .ConfigureAwait(false);

        if (hatExists)
            return new Result<CreateHatResponse>(new CreateHatResponse { HatId = hatId }, HttpStatusCode.OK);

        var newHat = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            HatName = request.HatName,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            OrganizerEmail = request.OrganizerEmail,
            OrganizerName = request.OrganizerName,
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        await _dynamoDbService
            .CreateHatAsync(newHat)
            .ConfigureAwait(false);

        return new Result<CreateHatResponse>(new CreateHatResponse { HatId = newHat.HatId }, HttpStatusCode.Created);
    }
}
