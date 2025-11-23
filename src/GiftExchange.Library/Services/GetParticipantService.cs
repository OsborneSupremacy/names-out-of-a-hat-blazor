namespace GiftExchange.Library.Services;

public class GetParticipantService : IBusinessService<GetParticipantRequest, Participant>
{
    private readonly GiftExchangeDataProvider _giftExchangeDataProvider;

    public GetParticipantService(GiftExchangeDataProvider giftExchangeDataProvider)
    {
        _giftExchangeDataProvider = giftExchangeDataProvider ?? throw new ArgumentNullException(nameof(giftExchangeDataProvider));
    }

    public async Task<Result<Participant>> ExecuteAsync(GetParticipantRequest request, ILambdaContext context)
    {
        var (participantExists, participant) = await _giftExchangeDataProvider
            .GetParticipantAsync(request.OrganizerEmail, request.HatId, request.ParticipantEmail)
            .ConfigureAwait(false);

        if (!participantExists)
            return new Result<Participant>(
                new KeyNotFoundException($"Participant with email {request.ParticipantEmail} not found"),
                HttpStatusCode.NotFound);

        if(!request.ShowPickedRecipients)
            participant = RedactPickedRecipient(participant);

        return new Result<Participant>(participant, HttpStatusCode.OK);
    }

    private Participant RedactPickedRecipient(Participant participant) =>
        participant with
        {
            PickedRecipient = string.IsNullOrWhiteSpace(participant.PickedRecipient)
                ? string.Empty
                : Persons.Redacted.Name
        };
}
