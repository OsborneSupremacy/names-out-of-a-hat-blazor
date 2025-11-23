namespace GiftExchange.Library.Services;

[UsedImplicitly]
public class EditParticipantService : IBusinessService<EditParticipantRequest, StatusCodeOnlyResponse>
{
    private readonly GiftExchangeDataProvider _giftExchangeDataProvider;

    public EditParticipantService(GiftExchangeDataProvider giftExchangeDataProvider)
    {
        _giftExchangeDataProvider = giftExchangeDataProvider ?? throw new ArgumentNullException(nameof(giftExchangeDataProvider));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(
        EditParticipantRequest request,
        ILambdaContext context
        )
    {
        var (hatExists, hat) = await _giftExchangeDataProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        var (participantExists, participant) = await _giftExchangeDataProvider.GetParticipantAsync(
            request.OrganizerEmail,
            request.HatId,
            request.Email
        ).ConfigureAwait(false);

        if(!participantExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Participant with email `{request.Email}` not found"), HttpStatusCode.NotFound);

        if(request.EligibleRecipients.Contains(participant.Person.Name, StringComparer.OrdinalIgnoreCase))
            return new Result<StatusCodeOnlyResponse>(new ArgumentException("Participant cannot set themselves as an eligible recipient"), HttpStatusCode.BadRequest);

        var otherParticipants = hat.Participants
            .Where(p => !p.Person.Name.ContentEquals(participant.Person.Name))
            .Select(p => p.Person.Name)
            .ToImmutableList();

        var invalidRecipients = request.EligibleRecipients
            .Where(r => !otherParticipants.Contains(r, StringComparer.OrdinalIgnoreCase))
            .ToImmutableList();

        if (invalidRecipients.Any())
        {
            var errorMessage = $"""
                                One or more provided recipients are not part of this gift exchange.

                                Gift exchange participants: {string.Join(", ", otherParticipants)}
                                Provided Recipients: {string.Join(", ", request.EligibleRecipients)}
                                Invalid Recipients: {string.Join(", ", invalidRecipients)}


                                """;

            return new Result<StatusCodeOnlyResponse>(new ArgumentException(errorMessage), HttpStatusCode.BadRequest);
        }

        await _giftExchangeDataProvider.UpdateEligibleRecipientsAsync(
            request.OrganizerEmail,
            request.HatId,
            request.Email,
            request.EligibleRecipients
        ).ConfigureAwait(false);

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }
}
