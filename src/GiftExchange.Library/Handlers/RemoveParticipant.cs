namespace GiftExchange.Library.Handlers;

public class RemoveParticipant
{
    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public RemoveParticipant()
    {
        _dynamoDbService = new DynamoDbService();
    }

    [UsedImplicitly]
    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler.FunctionHandler<RemoveParticipantRequest>
        (
            request,
            InnerHandler,
            context
        );

    private async Task<Result> InnerHandler(RemoveParticipantRequest request)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        var participantsOut = hat.Participants
            .Select(p => p with
            {
                Recipients = GetUpdatedRecipientList(p, request)
            })
            .ToList();

        var existingParticipant = participantsOut
            .FirstOrDefault(p => p.Person.Email.ContentEquals(request.Email));

        if(existingParticipant is null)
            return new Result(HttpStatusCode.OK);

        if(ParticipantIsOrganizer(existingParticipant, hat))
            return new Result(new InvalidOperationException("The organizer cannot be removed."), HttpStatusCode.BadRequest);

        participantsOut.Remove(existingParticipant);

        await _dynamoDbService
            .UpdateParticipantsAsync(
                request.OrganizerEmail,
                request.HatId,
                participantsOut.ToImmutableList()
            );

        return new Result(HttpStatusCode.OK);
    }

    private static bool ParticipantIsOrganizer(Participant participant, Hat hat) =>
        participant.Person.Email.ContentEquals(hat.Organizer.Email);

    private ImmutableList<Recipient> GetUpdatedRecipientList(Participant participant, RemoveParticipantRequest request)
    {
        if(participant.Person.Email.ContentEquals(request.Email))
            return [];

        var recipientsOut = participant
            .Recipients
            .ToList();

        var existingRecipient = recipientsOut
            .FirstOrDefault(r => r.Person.Email.ContentEquals(request.Email));

        if (existingRecipient is not null)
            recipientsOut.Remove(existingRecipient);

        return recipientsOut.ToImmutableList();
    }
}
