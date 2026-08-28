namespace GiftExchange.Library.Services;

[UsedImplicitly]
internal class EnqueueInvitationsService : IApiGatewayHandler
{
    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly ApiGatewayAdapter _adapter;

    private readonly EmailCompositionService _emailCompositionService;

    private readonly IEmailQueue _emailQueue;

    private readonly ISchedulerService _schedulerService;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    public EnqueueInvitationsService(
        GiftExchangeProvider giftExchangeProvider,
        ApiGatewayAdapter adapter,
        HatPreconditionValidator hatPreconditionValidator,
        EmailCompositionService emailCompositionService,
        IEmailQueue emailQueue,
        ISchedulerService schedulerService
        )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _emailCompositionService =
            emailCompositionService ?? throw new ArgumentNullException(nameof(emailCompositionService));
        _emailQueue = emailQueue ?? throw new ArgumentNullException(nameof(emailQueue));
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
    }

    // The address is read from the request context here rather than being carried on the request
    // body, so a caller cannot choose what gets recorded against their send.
    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context) =>
        _adapter.AdaptAsync<SendInvitationsRequest, StatusCodeOnlyResponse>(
            request,
            innerRequest => ExecuteAsync(innerRequest, request.GetSourceIpAddress()));

    internal async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(
        SendInvitationsRequest request,
        string sentFromIpAddress
    )
    {
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = [],
                ValidHatStatuses = [ HatStatus.NamesAssigned ]
            })
            .ConfigureAwait(false);

        if (!hatPreconditionResult.PreconditionsMet)
            return new Result<StatusCodeOnlyResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        var hat = hatPreconditionResult.Hat;

        // Before anything is queued, because the token has to be inside the invitation. Issued here
        // rather than when a participant is added: this is the first moment there is an email going
        // to them to carry it, and a token nobody has been told is only a row.
        var giftIdeasTokens = await _giftExchangeProvider
            .IssueGiftIdeaTokensAsync(request.HatId)
            .ConfigureAwait(false);

        var enqueueTasks = new List<Task>();

        foreach(var participant in hat.Participants)
        {
            // An address with no token issued against it gets an invitation without the block, not
            // a broken one. Nothing should reach this, since the tokens were just written for every
            // participant in the hat.
            var giftIdeasToken = giftIdeasTokens.GetValueOrDefault(participant.Person.Email, string.Empty);

            var invitation = new GiftExchangeEmailRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                HtmlBody = _emailCompositionService
                    .ComposeEmail(hat, participant.Person.Name, participant.PickedRecipient, giftIdeasToken),
                RecipientEmail = participant.Person.Email,
                Subject = EmailCompositionService.GetSubject(hat)
            };

            enqueueTasks.Add(_emailQueue.EnqueueAsync(invitation));
        }

        await Task.WhenAll(enqueueTasks)
            .ConfigureAwait(false);

        var invitationsQueuedAt = await _giftExchangeProvider
            .MarkInvitationsAsQueuedAsync(request.OrganizerEmail, request.HatId, sentFromIpAddress)
            .ConfigureAwait(false);

        await _schedulerService.CreateCooledOffScheduleAsync(request, invitationsQueuedAt)
            .ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
