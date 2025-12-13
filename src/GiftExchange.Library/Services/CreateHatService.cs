using GiftExchange.Library.DataModels;

namespace GiftExchange.Library.Services;

public class CreateHatService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly JsonService _jsonService;

    public CreateHatService(
        GiftExchangeProvider giftExchangeProvider,
        JsonService jsonService
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    )
    {
        var innerRequest = request.GetInnerRequest<CreateHatRequest>(_jsonService);

        if(innerRequest.IsFaulted)
            return ProxyResponseBuilder.Build(innerRequest.StatusCode, innerRequest.Exception.Message);

        var result = await CreateHatAsync(innerRequest.Value, context);

        return result.IsFaulted ?
            ProxyResponseBuilder.Build(result.StatusCode, result.Exception.Message) :
            ProxyResponseBuilder.Build(result.StatusCode, _jsonService.SerializeDefault(result.Value));
    }

    public async Task<Result<CreateHatResponse>> CreateHatAsync(CreateHatRequest request, ILambdaContext context)
    {
        var (hatExists, existingHatId) = await _giftExchangeProvider
            .DoesHatAlreadyExistAsync(request.OrganizerEmail, request.HatName)
            .ConfigureAwait(false);

        if (hatExists)
            return new Result<CreateHatResponse>(new CreateHatResponse { HatId = existingHatId }, HttpStatusCode.OK);

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

        var created = await _giftExchangeProvider
            .CreateHatAsync(newHat)
            .ConfigureAwait(false);

        if(!created)
            return new Result<CreateHatResponse>(new CreateHatResponse { HatId = newHat.HatId }, HttpStatusCode.OK);

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
