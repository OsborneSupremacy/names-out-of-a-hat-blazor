namespace GiftExchange.Library.Handlers;

public class EditParticipant
{
    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public EditParticipant()
    {
        _dynamoDbService = new DynamoDbService();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler.FunctionHandler<EditParticipantRequest>
        (
            request,
            InnerHandler,
            context
        );

    private async Task<Result> InnerHandler(EditParticipantRequest request)
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
            .FirstOrDefault(p => p.Person.Id == request.PersonId);

        if(existingParticipant is null)
            return new Result(new KeyNotFoundException($"Participant with id {request.PersonId} not found"), HttpStatusCode.NotFound);

        if(ParticipantIsOrganizer(existingParticipant, hat))
        {
            if(OrganizerEmailChanged(existingParticipant, request))
                return new Result(new InvalidOperationException("The organizer's email address cannot be changed."), HttpStatusCode.BadRequest);

            // the only thing that can be changed for the organizer is their name
            if(OrganizerNameChanged(existingParticipant, request))
                await _dynamoDbService.UpdateOrganizerNameAsync(
                    request.OrganizerEmail,
                    request.HatId,
                    request.Name
                ).ConfigureAwait(false);
        }

        participantsOut.Remove(existingParticipant);

        // check if a participant with the new email already exists
        if(participantsOut.Any(p => p.Person.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            return new Result(new InvalidOperationException($"Participant with email {request.Email} already exists"), HttpStatusCode.Conflict);

        var newRecipientList = GetUpdatedRecipientList(participantsOut, request);

        // re-add the updated participant with the new details
        participantsOut.Add(existingParticipant with
        {
            Person = existingParticipant.Person with
            {
                Name = request.Name,
                Email = request.Email,
            },
            Recipients = newRecipientList
        });

        await _dynamoDbService
            .UpdateParticipantsAsync(
                request.OrganizerEmail,
                request.HatId,
                participantsOut.ToImmutableList()
            );

        return new Result(HttpStatusCode.OK);
    }

    private static bool ParticipantIsOrganizer(Participant participant, Hat hat) =>
        participant.Person.Id == hat.Organizer.Id;

    private static bool OrganizerEmailChanged(Participant participant, EditParticipantRequest request) =>
        participant.Person.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase);

    private static bool OrganizerNameChanged(Participant participant, EditParticipantRequest request) =>
        !participant.Person.Name.Equals(request.Name);

    /// <summary>
    /// The update recipient will be in the list of every other participant. We need to update those items.
    ///
    /// Use this for the participants *not* being edited.
    /// </summary>
    private ImmutableList<Recipient> GetUpdatedRecipientList(Participant participant, EditParticipantRequest request)
    {
        if(participant.Person.Id == request.PersonId)
            return participant.Recipients;

        var recipientsOut = participant
            .Recipients
            .ToList();

        var existingRecipient = recipientsOut
            .FirstOrDefault(r => r.Person.Id == request.PersonId);

        if (existingRecipient is not null)
            recipientsOut.Remove(existingRecipient);

        recipientsOut.Add(new Recipient
        {
            Person = new Person
            {
                Id = request.PersonId,
                Name = request.Name,
                Email = request.Email
            },
            Eligible = existingRecipient?.Eligible ?? true
        });

        return recipientsOut.ToImmutableList();
    }

    /// <summary>
    /// Use this for the participant being edited.
    /// </summary>
    /// <returns></returns>
    private ImmutableList<Recipient> GetUpdatedRecipientList(List<Participant> otherParticipants, EditParticipantRequest request) =>
        otherParticipants
            .Select(p => new Recipient
            {
                Person = p.Person,
                Eligible = request.EligibleRecipientIds.Contains(p.Person.Id)
            })
            .ToImmutableList();
}
