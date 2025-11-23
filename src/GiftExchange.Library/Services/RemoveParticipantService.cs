namespace GiftExchange.Library.Services;

public class RemoveParticipantService : IBusinessService<RemoveParticipantRequest, StatusCodeOnlyResponse>
{
    ILogger<RemoveParticipantService> _logger;

    private readonly GiftExchangeDataProvider _giftExchangeDataProvider;

    public RemoveParticipantService(ILogger<RemoveParticipantService> logger, GiftExchangeDataProvider giftExchangeDataProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _giftExchangeDataProvider = giftExchangeDataProvider ?? throw new ArgumentNullException(nameof(giftExchangeDataProvider));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(RemoveParticipantRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _giftExchangeDataProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        var participantsOut = hat.Participants
            .Select(p => p with
            {
                EligibleRecipients = GetUpdatedRecipientList(p, request)
            })
            .ToList();

        var existingParticipant = participantsOut
            .FirstOrDefault(p => p.Person.Email.ContentEquals(request.Email));

        if(existingParticipant is null)
            return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK }, HttpStatusCode.OK);

        if(ParticipantIsOrganizer(existingParticipant, hat))
            return new Result<StatusCodeOnlyResponse>(new InvalidOperationException("The organizer cannot be removed."), HttpStatusCode.BadRequest);

        participantsOut.Remove(existingParticipant);
        //
        // await _giftExchangeDataProvider
        //     .UpdateParticipantsAsync(
        //         request.OrganizerEmail,
        //         request.HatId,
        //         participantsOut.ToImmutableList()
        //     );

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }

    private static bool ParticipantIsOrganizer(Participant participant, Hat hat) =>
        participant.Person.Email.ContentEquals(hat.Organizer.Email);


    private ImmutableList<string> GetUpdatedRecipientList(Participant participant, RemoveParticipantRequest request)
    {
        if(participant.Person.Email.ContentEquals(request.Email))
            return [];

        var recipientsOut = participant
            .EligibleRecipients
            .ToList();

        var existingRecipient = recipientsOut
            .FirstOrDefault(r => r.ContentEquals(request.Email));

        if (existingRecipient is not null)
            recipientsOut.Remove(existingRecipient);

        return recipientsOut.ToImmutableList();
    }
}
