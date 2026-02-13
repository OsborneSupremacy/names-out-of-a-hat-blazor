namespace GiftExchange.Library.Services;

internal class HatPreconditionValidator
{
    private readonly ILogger<HatPreconditionValidator> _logger;

    private readonly IContentModerationService _contentModerationService;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public HatPreconditionValidator(
        ILogger<HatPreconditionValidator> logger,
        GiftExchangeProvider giftExchangeProvider,
        IContentModerationService contentModerationService
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _contentModerationService = contentModerationService ?? throw new ArgumentNullException(nameof(contentModerationService));
    }

    public async Task<HatPreconditionResponse> ValidateAsync(HatPreconditionRequest request)
    {
        if (request.FieldsToModerate.Any())
        {
            var (isAcceptable, errorMessage) = await _contentModerationService
                .ValidateMultipleFieldsAsync(request.FieldsToModerate)
                .ConfigureAwait(false);

            if (!isAcceptable)
                return new HatPreconditionResponse
                {
                    PreconditionsMet = false,
                    PreconditionFailureMessage = new PreconditionFailureMessage
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        FailureMessage = string.Join(';', errorMessage)
                    },
                    Hat = Hats.Empty
                };
        }

        var (hatExists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if (!hatExists)
            return new HatPreconditionResponse
            {
                PreconditionsMet = false,
                PreconditionFailureMessage = new PreconditionFailureMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    FailureMessage = $"Hat with id {request.HatId} not found"
                },
                Hat = Hats.Empty
            };

        if (!request.ValidHatStatuses.Contains(hat.Status))
            return new HatPreconditionResponse
            {
                PreconditionsMet = false,
                PreconditionFailureMessage = new PreconditionFailureMessage
                {
                    StatusCode = HttpStatusCode.Conflict,
                    FailureMessage = $"Hat status {hat.Status} is not valid for this operation"
                },
                Hat = Hats.Empty
            };

        return new HatPreconditionResponse
        {
            PreconditionsMet = true,
            PreconditionFailureMessage = PreconditionFailureMessages.Empty,
            Hat = hat
        };
    }
}
