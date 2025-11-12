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
            .ToList();

        var existingParticipant = participantsOut
            .FirstOrDefault(p => p.Person.Email == request.Email);

        if(existingParticipant is null)
            return new Result(new KeyNotFoundException($"Participant with email `{request.Email}` not found"), HttpStatusCode.NotFound);

        participantsOut.Remove(existingParticipant);

        // check if a participant with the new email already exists
        if(participantsOut.Any(p => p.Person.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            return new Result(new InvalidOperationException($"Participant with email {request.Email} already exists"), HttpStatusCode.Conflict);

        var newRecipientList = GetUpdatedRecipientList(participantsOut, request);

        // re-add the updated participant with the new details
        participantsOut.Add(existingParticipant with
        {
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

    /// <summary>
    /// Use this for the participant being edited.
    /// </summary>
    /// <returns></returns>
    private ImmutableList<Recipient> GetUpdatedRecipientList(List<Participant> otherParticipants, EditParticipantRequest request) =>
        otherParticipants
            .Select(p => new Recipient
            {
                Person = p.Person,
                Eligible = request.EligibleRecipientEmails.Contains(p.Person.Email, StringComparer.OrdinalIgnoreCase)
            })
            .ToImmutableList();
}
