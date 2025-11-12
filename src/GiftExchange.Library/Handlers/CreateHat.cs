namespace GiftExchange.Library.Handlers;

public class CreateHat
{
    private readonly DynamoDbService _dynamoDbService;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public CreateHat()
    {
        _dynamoDbService = new DynamoDbService();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context
    ) =>
        await PayloadHandler.FunctionHandler<CreateHatRequest, CreateHatResponse>
        (
            request,
            InnerHandler,
            context
        );

    private async Task<Result<CreateHatResponse>> InnerHandler(CreateHatRequest request)
    {
        var (hatExists, hatId) = await _dynamoDbService
            .DoesHatExistAsync(request.OrganizerEmail)
            .ConfigureAwait(false);

        if (hatExists)
            return new Result<CreateHatResponse>(new CreateHatResponse { HatId = hatId }, HttpStatusCode.OK);

        var organizer = new Person { Name = request.OrganizerName, Email = request.OrganizerEmail };

        var organizerParticipant = new Participant
        {
            PickedRecipient = Persons.Empty,
            Person = organizer,
            InvitationSent = false,
            Recipients = []
        };

        var newHat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = request.HatName,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = organizer,
            Participants = [ organizerParticipant ],
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        var newHatId = await _dynamoDbService
            .CreateHatAsync(newHat)
            .ConfigureAwait(false);

        return new Result<CreateHatResponse>(new CreateHatResponse { HatId = newHatId }, HttpStatusCode.Created);
    }
}
