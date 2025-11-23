using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Services;

public class CreateHatService : IBusinessService<CreateHatRequest, CreateHatResponse>
{
    private readonly GiftExchangeDataProvider _giftExchangeDataProvider;

    public CreateHatService(GiftExchangeDataProvider giftExchangeDataProvider)
    {
        _giftExchangeDataProvider = giftExchangeDataProvider ?? throw new ArgumentNullException(nameof(giftExchangeDataProvider));
    }

    public async Task<Result<CreateHatResponse>> ExecuteAsync(CreateHatRequest request, ILambdaContext context)
    {
        var (hatExists, hatId) = await _giftExchangeDataProvider
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

        await _giftExchangeDataProvider
            .CreateHatAsync(newHat)
            .ConfigureAwait(false);

        await _giftExchangeDataProvider
            .CreateParticipantAsync(new AddParticipantRequest
            {
                HatId = newHat.HatId,
                OrganizerEmail = request.OrganizerEmail,
                Name = request.OrganizerName,
                Email = request.OrganizerEmail
            }, []);

        return new Result<CreateHatResponse>(new CreateHatResponse { HatId = newHat.HatId }, HttpStatusCode.Created);
    }
}
