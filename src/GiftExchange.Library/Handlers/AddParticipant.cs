namespace GiftExchange.Library.Handlers;

public class AddParticipant
{
    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public AddParticipant()
    {
        _dynamoDbService = new DynamoDbService();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler
            .FunctionHandler<AddParticipantRequest>(
                request,
                InnerHandler,
                context
            );

    private async Task<Result> InnerHandler(AddParticipantRequest request)
    {
        var (hatExists, hat) = await _dynamoDbService
            .GetHatAsync(request.OrganizerEmail, request.HatId).ConfigureAwait(false);

        if(!hatExists)
            return new Result(new KeyNotFoundException($"Hat with id {request.HatId} not found"), HttpStatusCode.NotFound);

        // Check if a participant with the same email already exists
        if(hat.Participants.Any(p => p.Person.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            return new Result(new InvalidOperationException($"Participant with email {request.Email} already exists in the hat."), HttpStatusCode.Conflict);

        var newPerson = new Person
        {
            Name = request.Name,
            Email = request.Email,
        };

        var newParticipant = new Participant
        {
            Person = newPerson,
            PickedRecipient = Persons.Empty,
            InvitationSent = false,
            Recipients = hat.Participants
                .Select(p => new Recipient
                {
                    Person = p.Person,
                    Eligible = true
                }).ToImmutableList()
        };

        var participantsOut = hat.Participants
            .Select(p => p with
            {
                Recipients = p.Recipients
                    .Concat([new Recipient
                    {
                        Person = newPerson,
                        Eligible = true
                    }])
                    .ToImmutableList()
            })
            .Concat([newParticipant])
            .ToImmutableList();

        await _dynamoDbService.UpdateParticipantsAsync(
            request.OrganizerEmail,
            request.HatId,
            participantsOut
        ) .ConfigureAwait(false);

        return new Result(HttpStatusCode.Created);
    }
}
