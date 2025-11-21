namespace GiftExchange.Library.Services;

[UsedImplicitly]
public class EditParticipantService : IBusinessService<EditParticipantRequest, StatusCodeOnlyResponse>
{
    private readonly DynamoDbService _dynamoDbService;

    public EditParticipantService(DynamoDbService dynamoDbService)
    {
        _dynamoDbService = dynamoDbService ?? throw new ArgumentNullException(nameof(dynamoDbService));
    }

    public async Task<Result<StatusCodeOnlyResponse>> ExecuteAsync(EditParticipantRequest request, ILambdaContext context)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        var participantsOut = hat.Participants
            .ToList();

        var existingParticipant = participantsOut
            .FirstOrDefault(p => p.Person.Email.ContentEquals(request.Email));

        if(existingParticipant is null)
            return new Result<StatusCodeOnlyResponse>(new KeyNotFoundException($"Participant with email `{request.Email}` not found"), HttpStatusCode.NotFound);

        participantsOut.Remove(existingParticipant);

        // check if a participant with the new email already exists
        if(participantsOut.Any(p => p.Person.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            return new Result<StatusCodeOnlyResponse>(new InvalidOperationException($"Participant with email {request.Email} already exists"), HttpStatusCode.Conflict);

        var newRecipientList = GetUpdatedEligibleRecipientNames(
            participantsOut.Select(p => p.Person.Name).ToList(),
            request
        );

        // re-add the updated participant with the new details
        participantsOut.Add(existingParticipant with
        {
            EligibleRecipients = newRecipientList
        });

        // await _dynamoDbService
        //     .UpdateParticipantsAsync(
        //         request.OrganizerEmail,
        //         request.HatId,
        //         participantsOut.ToImmutableList()
        //     );

        return new Result<StatusCodeOnlyResponse>(new StatusCodeOnlyResponse { StatusCode = HttpStatusCode.OK}, HttpStatusCode.OK);
    }

    /// <summary>
    /// Use this for the participant being edited.
    /// </summary>
    /// <returns></returns>
    private ImmutableList<string> GetUpdatedEligibleRecipientNames(List<string> otherParticipants, EditParticipantRequest request) =>
        otherParticipants
            .Select(r => r)
            .Where(r => request.EligibleRecipientEmails.Contains(r, StringComparer.OrdinalIgnoreCase))
            .ToImmutableList();
}
