using GiftExchange.Library.Utility;

namespace GiftExchange.Library.Tests.HandlerTests;

/// <summary>
/// Renaming a participant, against a real database.
///
/// Two things are being pinned down. The first is that nothing about the draw moves: eligibility
/// and picks are participant ids, so a rename after the hat is shaken has to leave the hat shaken
/// and the pick intact — which is precisely what removing and re-adding somebody, the only remedy
/// an organizer had before, does not do.
///
/// The second is reach. A name is one row on one person, so renaming somebody is felt in every
/// exchange they take part in. That is deliberate, and it is also what makes the collision check
/// wider than it looks: a name free in this hat can be taken in one this organizer cannot see.
/// </summary>
[Collection(PostgresCollection.Name)]
public class EditParticipantNameTests
{
    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    private readonly GiftExchangeProvider _provider;

    private readonly TestDataService _testDataService;

    private readonly AddParticipantRequestFaker _participantFaker = new();

    private readonly IApiGatewayHandler _sut;

    public EditParticipantNameTests(PostgresFixture dbFixture)
    {
        DotEnv.Load();

        var contextFactory = dbFixture.CreateContextFactory();
        _context = new FakeLambdaContext();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(contextFactory)
            .AddSingleton<IContentModerationService, FakeContentModerationService>()
            .BuildServiceProvider();

        _jsonService = serviceProvider.GetRequiredService<JsonService>();
        _provider = serviceProvider.GetRequiredService<GiftExchangeProvider>();
        _testDataService = new TestDataService(_provider);

        _sut = serviceProvider.GetRequiredKeyedService<IApiGatewayHandler>("put/participant/name");
    }

    [Fact]
    public async Task EditName_RenamesTheParticipant()
    {
        // arrange
        var hat = await CreateHatWithOrganizerAsync();
        var participant = await AddParticipantAsync(hat, "Original Name");

        // act
        var response = await RenameAsync(hat.Organizer.Email, hat.Id, participant.Email, "Corrected Name");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        var stored = await _testDataService.GetParticipantAsync(hat.Organizer.Email, hat.Id, participant.Email);
        stored.Person.Name.Should().Be("Corrected Name");
    }

    /// <summary>
    /// The reason this endpoint exists apart from PUT /participant, which resets the hat to
    /// IN_PROGRESS as it edits eligibility. A rename touches nothing the draw is made of, so a
    /// shaken hat has to stay shaken and the pick has to survive.
    /// </summary>
    [Fact]
    public async Task EditName_AfterTheHatIsShaken_LeavesTheDrawAlone()
    {
        // arrange
        var hat = await CreateHatWithOrganizerAsync();
        var giver = await AddParticipantAsync(hat, "Giver");
        var receiver = await AddParticipantAsync(hat, "Receiver");

        await _provider.UpdateParticipantPickedRecipientAsync(
            hat.Organizer.Email, hat.Id, giver.Email, "Receiver");

        await _provider.UpdateHatStatusAsync(hat.Organizer.Email, hat.Id, HatStatus.NamesAssigned);

        // act
        var response = await RenameAsync(hat.Organizer.Email, hat.Id, receiver.Email, "Receiver Renamed");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        var stored = await _testDataService.GetHatAsync(hat.Organizer.Email, hat.Id);

        stored.Status.Should().Be(HatStatus.NamesAssigned);

        // The pick is stored as an id and projected back through the person row, so it follows the
        // rename rather than pointing at a name nobody answers to any more.
        stored.Participants
            .Single(candidate => candidate.Person.Email == giver.Email)
            .PickedRecipient.Should().Be("Receiver Renamed");
    }

    /// <summary>
    /// A name belongs to the person rather than to one membership, so it moves everywhere at once
    /// — the same property <c>UpdateProfileService</c> has when somebody renames themselves.
    /// </summary>
    [Fact]
    public async Task EditName_RenamesThemInEveryExchangeTheyAreIn()
    {
        // arrange: Bob is in Alice's exchange and in Carol's.
        var alicesHat = await CreateHatWithOrganizerAsync();
        var carolsHat = await CreateHatWithOrganizerAsync();

        var bob = await AddParticipantAsync(alicesHat, "Bob Original");
        await AddParticipantAsync(carolsHat, "Bob Original", bob.Email);

        // act: Alice renames him in hers.
        var response = await RenameAsync(alicesHat.Organizer.Email, alicesHat.Id, bob.Email, "Bob Renamed");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        var inCarols = await _testDataService
            .GetParticipantAsync(carolsHat.Organizer.Email, carolsHat.Id, bob.Email);

        inCarols.Person.Name.Should().Be("Bob Renamed");
    }

    [Fact]
    public async Task EditName_GivenANameAnotherParticipantHere_ReturnsConflictAndNamesTheExchange()
    {
        // arrange
        var hat = await CreateHatWithOrganizerAsync();
        var participant = await AddParticipantAsync(hat, "Original Name");
        await AddParticipantAsync(hat, "Taken Name");

        // act: compared case-insensitively, the same way AddParticipantService compares names.
        var response = await RenameAsync(hat.Organizer.Email, hat.Id, participant.Email, "taken name");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        response.Body.Should().Contain(hat.Name);

        var stored = await _testDataService.GetParticipantAsync(hat.Organizer.Email, hat.Id, participant.Email);
        stored.Person.Name.Should().Be("Original Name");
    }

    /// <summary>
    /// The collision is in an exchange the caller does not run, and the rename would have reached
    /// it. Refused, and explained — but the other organizer's exchange is not named, because whose
    /// guest list it collided with is not this organizer's to learn.
    /// </summary>
    [Fact]
    public async Task EditName_GivenANameTakenInSomebodyElsesExchange_ReturnsConflictWithoutNamingIt()
    {
        // arrange: Bob is in Alice's exchange and in Carol's, and Carol's already has a Dave.
        var alicesHat = await CreateHatWithOrganizerAsync();
        var carolsHat = await CreateHatWithOrganizerAsync();

        var bob = await AddParticipantAsync(alicesHat, "Bob Original");
        await AddParticipantAsync(carolsHat, "Bob Original", bob.Email);
        await AddParticipantAsync(carolsHat, "Dave");

        // act
        var response = await RenameAsync(alicesHat.Organizer.Email, alicesHat.Id, bob.Email, "Dave");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        response.Body.Should().NotContain(carolsHat.Name);

        var stored = await _testDataService
            .GetParticipantAsync(alicesHat.Organizer.Email, alicesHat.Id, bob.Email);

        stored.Person.Name.Should().Be("Bob Original");
    }

    /// <summary>
    /// Nobody collides with themselves. The person being renamed is excluded from the check by
    /// person id, which is what makes fixing the case of a name an accepted edit rather than a
    /// conflict with the row about to be overwritten.
    /// </summary>
    [Fact]
    public async Task EditName_ChangingOnlyTheCapitalisation_IsAccepted()
    {
        // arrange
        var hat = await CreateHatWithOrganizerAsync();
        var participant = await AddParticipantAsync(hat, "bob mcbobface");

        // act
        var response = await RenameAsync(hat.Organizer.Email, hat.Id, participant.Email, "Bob McBobface");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        var stored = await _testDataService.GetParticipantAsync(hat.Organizer.Email, hat.Id, participant.Email);
        stored.Person.Name.Should().Be("Bob McBobface");
    }

    /// <summary>
    /// The organizer is a participant of their own exchange, and both rows point at the same
    /// person, so renaming themselves through this endpoint moves the name the hat is organized
    /// under too.
    /// </summary>
    [Fact]
    public async Task EditName_AppliedToTheOrganizer_MovesTheNameTheHatIsOrganizedUnder()
    {
        // arrange
        var hat = await CreateHatWithOrganizerAsync();

        // act
        var response = await RenameAsync(hat.Organizer.Email, hat.Id, hat.Organizer.Email, "Organizer Renamed");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        var stored = await _testDataService.GetHatAsync(hat.Organizer.Email, hat.Id);
        stored.Organizer.Name.Should().Be("Organizer Renamed");
    }

    [Fact]
    public async Task EditName_ForSomebodyNotInTheExchange_ReturnsNotFound()
    {
        // arrange
        var hat = await CreateHatWithOrganizerAsync();

        // act
        var response = await RenameAsync(hat.Organizer.Email, hat.Id, "nobody@example.com", "Anybody");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Ownership is established by the precondition validator, and a caller who does not organize
    /// this exchange cannot see it at all — so the answer is that there is no such hat rather than
    /// that they may not touch it.
    /// </summary>
    [Fact]
    public async Task EditName_ByAnOrganizerWhoDoesNotOwnTheHat_ReturnsNotFound()
    {
        // arrange
        var alicesHat = await CreateHatWithOrganizerAsync();
        var carolsHat = await CreateHatWithOrganizerAsync();
        var bob = await AddParticipantAsync(alicesHat, "Bob Original");

        // act
        var response = await RenameAsync(carolsHat.Organizer.Email, alicesHat.Id, bob.Email, "Bob Renamed");

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);

        var stored = await _testDataService
            .GetParticipantAsync(alicesHat.Organizer.Email, alicesHat.Id, bob.Email);

        stored.Person.Name.Should().Be("Bob Original");
    }

    private Task<APIGatewayProxyResponse> RenameAsync(
        string organizerEmail,
        Guid hatId,
        string participantEmail,
        string name
    ) =>
        _sut.FunctionHandler(
            _jsonService
                .SerializeDefault(new EditParticipantNameRequest
                {
                    OrganizerEmail = organizerEmail,
                    HatId = hatId,
                    Email = participantEmail,
                    Name = name
                })
                .ToApiGatewayProxyRequest(),
            _context);

    /// <summary>
    /// Mirrors CreateHatService: the organizer is a participant in their own exchange, and both
    /// rows point at the same person.
    /// </summary>
    private async Task<Hat> CreateHatWithOrganizerAsync()
    {
        var hat = await _testDataService.CreateTestHatAsync();

        await _testDataService.CreateParticipantAsync(new AddParticipantRequest
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Name = hat.Organizer.Name,
            Email = hat.Organizer.Email
        }, []);

        return hat;
    }

    private async Task<Person> AddParticipantAsync(Hat hat, string name, string? email = null)
    {
        var request = _participantFaker.Generate() with
        {
            OrganizerEmail = hat.Organizer.Email,
            HatId = hat.Id,
            Name = name
        };

        if (email is not null)
            request = request with { Email = email };

        var existing = await _provider.GetParticipantsAsync(hat.Organizer.Email, hat.Id);

        await _testDataService.CreateParticipantAsync(request, existing);

        return new Person { Name = request.Name, Email = request.Email };
    }
}
