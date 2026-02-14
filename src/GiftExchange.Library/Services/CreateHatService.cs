namespace GiftExchange.Library.Services;

internal class CreateHatService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly IContentModerationService _contentModerationService;

    private readonly ApiGatewayAdapter _adapter;

    public CreateHatService(
        GiftExchangeProvider giftExchangeProvider,
        IContentModerationService contentModerationService,
        ApiGatewayAdapter adapter
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _contentModerationService = contentModerationService ?? throw new ArgumentNullException(nameof(contentModerationService));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync<CreateHatRequest, CreateHatResponse>(request, CreateHatAsync);

    internal async Task<Result<CreateHatResponse>> CreateHatAsync(CreateHatRequest request)
    {
        // Validate content before processing
        var (isValid, errorMessages) = await _contentModerationService.ValidateMultipleFieldsAsync(
            new Dictionary<string, string>
            {
                ["gift exchange name"] = request.HatName,
                ["organizer name"] = request.OrganizerName
            });

        if (!isValid)
            return new Result<CreateHatResponse>(
                new InvalidOperationException(string.Join(" ", errorMessages)),
                HttpStatusCode.BadRequest
            );

        var (hatExists, existingHatId) = await _giftExchangeProvider
            .DoesHatAlreadyExistAsync(request.OrganizerEmail, request.HatName)
            .ConfigureAwait(false);

        if (hatExists)
            return new Result<CreateHatResponse>(new CreateHatResponse { HatId = existingHatId }, HttpStatusCode.OK);

        var newHat = new HatDataModel
        {
            HatId = Guid.NewGuid(),
            HatName = request.HatName,
            Status = HatStatus.InProgress,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            OrganizerEmail = request.OrganizerEmail,
            OrganizerName = request.OrganizerName,
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
