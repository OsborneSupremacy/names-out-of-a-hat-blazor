using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Services;

public class CreateHatService : IBusinessService<CreateHatRequest, CreateHatResponse>
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    public CreateHatService(GiftExchangeProvider giftExchangeProvider)
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    public async Task<Result<CreateHatResponse>> ExecuteAsync(CreateHatRequest request, ILambdaContext context)
    {
        var (hatExists, hatId) = await _giftExchangeProvider
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

        await _giftExchangeProvider
            .CreateHatAsync(newHat)
            .ConfigureAwait(false);

        await _giftExchangeProvider
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
