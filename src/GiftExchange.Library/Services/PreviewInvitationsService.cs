namespace GiftExchange.Library.Services;

[UsedImplicitly]
internal class PreviewInvitationsService : IApiGatewayHandler
{
    private const string PlaceholderParticipantName = "[Participant Name]";
    private const string PlaceholderPickedName = "[Picked Name]";

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
        var organizerEmail = request.GetEmailPathParameter();
        var hatId = request.GetIdPathParameter();

        return new Result<PreviewInvitationsRequest>(new PreviewInvitationsRequest
        {
            HatId = hatId,
            OrganizerEmail = organizerEmail
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
            HtmlBody = _emailCompositionService.ComposeEmail(hat, PlaceholderParticipantName, PlaceholderPickedName)
        };

        return new Result<PreviewInvitationsResponse>(preview, HttpStatusCode.OK);
    }
}
