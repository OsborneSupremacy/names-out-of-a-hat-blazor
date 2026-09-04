namespace GiftExchange.Library.Services;

[UsedImplicitly]
internal class PreviewInvitationsService : IApiGatewayHandler
{
    private const string PlaceholderParticipantName = "[Participant Name]";
    private const string PlaceholderPickedName = "[Picked Name]";

    /// <summary>
    /// Stands in for the routing token so the organizer sees the gift ideas block as participants
    /// will. Deliberately not a real token: a preview is composed for whoever is looking at it, and
    /// issuing one here would hand the organizer an address that writes to somebody else's row.
    /// </summary>
    private const string PlaceholderGiftIdeasToken = "example-token";

    /// <summary>
    /// Stands in for the leave token, so the organizer sees the leave sentence in the fine print
    /// exactly as their participants will.
    /// </summary>
    /// <remarks>
    /// Shown rather than omitted, deliberately. An organizer previewing an invitation is entitled
    /// to know that it offers a way out — it is their exchange, and finding out from the first
    /// person who uses it would be worse for everybody. Not a real token for the reason the gift
    /// ideas placeholder is not one, and more so: a working leave link handed to the organizer
    /// would remove a participant.
    /// </remarks>
    private const string PlaceholderLeaveToken = "example-token";

    private readonly ApiGatewayAdapter _adapter;

    private readonly HatPreconditionValidator _hatPreconditionValidator;

    private readonly EmailCompositionService _emailCompositionService;

    public PreviewInvitationsService(
        ApiGatewayAdapter adapter,
        HatPreconditionValidator hatPreconditionValidator,
        EmailCompositionService emailCompositionService
        )
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _hatPreconditionValidator = hatPreconditionValidator ?? throw new ArgumentNullException(nameof(hatPreconditionValidator));
        _emailCompositionService = emailCompositionService ?? throw new ArgumentNullException(nameof(emailCompositionService));
    }

    public Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var innerRequest = GetInnerRequest(request);
        return _adapter.AdaptAsync(innerRequest.Value, ExecuteAsync);
    }

    private static Result<PreviewInvitationsRequest> GetInnerRequest(APIGatewayProxyRequest request)
    {
        var organizerEmail = request.GetAuthenticatedEmail();
        var hatId = request.GetIdPathParameter();

        return new Result<PreviewInvitationsRequest>(new PreviewInvitationsRequest
        {
            HatId = hatId,
            OrganizerEmail = organizerEmail,
            SenderIpAddress = request.GetSourceIpAddress()
        }, HttpStatusCode.OK);
    }

    internal async Task<Result<PreviewInvitationsResponse>> ExecuteAsync(PreviewInvitationsRequest request)
    {
        var hatPreconditionResult = await _hatPreconditionValidator
            .ValidateAsync(new HatPreconditionRequest
            {
                HatId = request.HatId,
                OrganizerEmail = request.OrganizerEmail,
                FieldsToModerate = [],
                ValidHatStatuses = HatStatuses.All
            })
            .ConfigureAwait(false);

        if(!hatPreconditionResult.PreconditionsMet)
            return new Result<PreviewInvitationsResponse>(
                new AggregateException(hatPreconditionResult.PreconditionFailureMessage.FailureMessage),
                hatPreconditionResult.PreconditionFailureMessage.StatusCode);

        var hat = hatPreconditionResult.Hat;

        var preview = new PreviewInvitationsResponse
        {
            Subject = EmailCompositionService.GetSubject(hat),
            HtmlBody = _emailCompositionService.ComposeEmail(new ComposeInvitationRequest
            {
                Hat = hat,
                ParticipantName = PlaceholderParticipantName,
                PickedName = PlaceholderPickedName,
                GiftIdeasToken = PlaceholderGiftIdeasToken,
                LeaveToken = PlaceholderLeaveToken
            }),
            SenderIpAddress = request.SenderIpAddress
        };

        return new Result<PreviewInvitationsResponse>(preview, HttpStatusCode.OK);
    }
}
