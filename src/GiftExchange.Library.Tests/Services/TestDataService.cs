
namespace GiftExchange.Library.Tests.Services;

internal class TestDataService
{
    private readonly GiftExchangeDataProvider _giftExchangeDataProvider;

    private readonly HatDataModelFaker _hatDataModelFaker;

    public TestDataService(
        GiftExchangeDataProvider giftExchangeDataProvider
        )
    {
        _giftExchangeDataProvider = giftExchangeDataProvider ?? throw new ArgumentNullException(nameof(giftExchangeDataProvider));
        _hatDataModelFaker = new HatDataModelFaker();
    }

    public Task<bool> CreateHatAsync(HatDataModel newHat) =>
        _giftExchangeDataProvider.CreateHatAsync(newHat);

    public async Task<Hat> CreateTestHatAsync()
    {
        var newHat = _hatDataModelFaker.Generate();

        await _giftExchangeDataProvider.CreateHatAsync(newHat);

        return new Hat
        {
            Id = newHat.HatId,
            Name = newHat.HatName,
            AdditionalInformation = newHat.AdditionalInformation,
            PriceRange = newHat.PriceRange,
            OrganizerVerified = newHat.OrganizerVerified,
            RecipientsAssigned = newHat.RecipientsAssigned,
            Organizer = new Person {
                Email = newHat.OrganizerEmail,
                Name = newHat.OrganizerName
            },
            Participants = []
        };
    }

    public async Task<Hat> GetHatAsync(string organizerEmail, Guid hatId)
    {
        var (_, hat) = await _giftExchangeDataProvider
            .GetHatAsync(organizerEmail, hatId);
        return hat;
    }

    public Task<bool> CreateParticipantAsync(
        AddParticipantRequest request,
        ImmutableList<Participant> existingParticipants
        ) => _giftExchangeDataProvider.CreateParticipantAsync(request, existingParticipants);

    public async Task<Participant> GetParticipantAsync(
        string organizerEmail,
        Guid hatId,
        string participantUtEmail
        )
    {
        var (_, participant) = await _giftExchangeDataProvider
            .GetParticipantAsync(organizerEmail, hatId, participantUtEmail);
        return participant;
    }
}
