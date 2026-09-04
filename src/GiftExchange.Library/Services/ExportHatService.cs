namespace GiftExchange.Library.Services;

/// <summary>
/// Hands the organizer their gift exchange as data: everybody in it, who may draw whom, and — once
/// the picks are out — who drew whom.
/// </summary>
internal class ExportHatService : IApiGatewayHandler
{
    /// <summary>
    /// The shape of the document this produces. Bumped when a field changes meaning or leaves;
    /// adding one does not bump it, since anything reading an older export still reads a newer one.
    /// </summary>
    private const string FormatVersion = "1";

    private readonly ApiGatewayAdapter _adapter;

    private readonly GiftExchangeProvider _giftExchangeProvider;

    public ExportHatService(ApiGatewayAdapter adapter, GiftExchangeProvider giftExchangeProvider)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
    }

    /// <summary>
    /// A GET with the exchange in the path, like the other reads. The organizer comes from the
    /// authorizer rather than from the <c>{email}</c> segment, which is there so the route reads
    /// the same way its neighbours do.
    /// </summary>
    public Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        _adapter.AdaptAsync(
            new ExportHatRequest
            {
                OrganizerEmail = request.GetAuthenticatedEmail(),
                HatId = request.GetIdPathParameter()
            },
            ExportHatAsync);

    internal async Task<Result<ExportHatResponse>> ExportHatAsync(ExportHatRequest request)
    {
        var exported = await _giftExchangeProvider
            .ExportHatAsync(request)
            .ConfigureAwait(false);

        if (!exported.Exists)
            return new Result<ExportHatResponse>(
                new KeyNotFoundException($"Hat with id {request.HatId} not found"),
                HttpStatusCode.NotFound);

        return new Result<ExportHatResponse>(
            new ExportHatResponse
            {
                FormatVersion = FormatVersion,
                ExportedAt = DateTimeOffset.UtcNow,
                Hat = exported.Hat.Status == HatStatus.Closed
                    ? exported.Hat
                    : WithoutPicks(exported.Hat)
            },
            HttpStatusCode.OK);
    }

    /// <summary>
    /// Takes the draw back out of an exchange that has not revealed it.
    /// </summary>
    /// <remarks>
    /// The same rule <c>GetHatService.RedactPickedRecipients</c> applies to the detail view, and it
    /// has to be the same rule: an export is another way of asking who drew whom, and a second way
    /// of asking that answered differently would not be a feature, it would be the way around the
    /// first one. Emptied rather than replaced with a placeholder, because a file is read by
    /// machines as well as people and "Hidden" is a name somebody could be called.
    /// </remarks>
    private static ExportedHat WithoutPicks(ExportedHat hat) =>
        hat with
        {
            Participants =
            [
                .. hat.Participants
                    .Select(participant => participant with
                    {
                        PickedRecipient = ExportedParticipantReferences.Empty
                    })
            ]
        };
}
