namespace GiftExchange.Library.Tests.ServiceTests;

public class ValidationServiceTests : IClassFixture<DynamoDbFixture>
{
    private readonly PersonFaker _personFaker;


    private readonly ValidationService _sut;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public ValidationServiceTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        var dynamoDbClient = dbFixture.CreateClient();

        _personFaker = new PersonFaker();

        IServiceProvider serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(dynamoDbClient)
            .AddSingleton<IContentModerationService, FakeContentModerationService>()
            .BuildServiceProvider();

        _sut = (serviceProvider
            .GetRequiredKeyedService<IApiGatewayHandler>("post/hat/validate") as ValidationService)!;
    }

    [Fact]
    public async Task Validate_GivenHatWithTooFewParticipants_ReturnsInvalidResult()
    {
        // arrange
        var hat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            Status = HatStatus.InProgress,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = Persons.Empty,
            Participants = [
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = _personFaker.Generate(),
                    EligibleRecipients = []
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = _personFaker.Generate(),
                    EligibleRecipients = []
                }
            ],
            RecipientsAssigned = false,
            InvitationsQueued = false,
            InvitationsQueuedDate = DateTimeOffset.MinValue
        };

        // act
        var result = await _sut.ValidateAsync(hat);

        // assert
        result.Value.Success.Should().BeFalse();
        result.Value.Errors.First().Should().Be("A gift exchange of this type needs at least three people.");
    }

    [Fact]
    public async Task Validate_GivenParticipantWithNoEligibleParticipants_ReturnsInvalidResult()
    {
        var people = _personFaker.Generate(3);

        // arrange
        var hat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            Status = HatStatus.InProgress,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = Persons.Empty,
            Participants = [
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[0],
                    EligibleRecipients = [ people[1].Name, people[2].Name ]
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[1],
                    EligibleRecipients = [ people[0].Name, people[2].Name ]
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[2],
                    EligibleRecipients = []
                }
            ],
            RecipientsAssigned = false,
            InvitationsQueued = false,
            InvitationsQueuedDate = DateTimeOffset.MinValue
        };

        // act
        var result = await _sut.ValidateAsync(hat);

        // assert
        result.Value.Success.Should().BeFalse();
        result.Value.Errors.First().Should().Be($"{people[2].Name} has no eligible recipients");
    }

    [Fact]
    public async Task Validate_GivenParticipantWhoIsNotAnEligibleRecipient_ReturnsInvalidResult()
    {
        var people = _personFaker.Generate(4);

        // arrange
        var hat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            Status = HatStatus.InProgress,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = Persons.Empty,
            Participants = [
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[0],
                    EligibleRecipients = [ people[1].Name, people[2].Name ]
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[1],
                    EligibleRecipients = [ people[0].Name, people[2].Name ]
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[2],
                    EligibleRecipients = [ people[0].Name, people[1].Name ]
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[3],
                    EligibleRecipients = [ people[1].Name, people[2].Name ]
                }
            ],
            RecipientsAssigned = false,
            InvitationsQueued = false,
            InvitationsQueuedDate = DateTimeOffset.MinValue
        };

        // act
        var result = await _sut.ValidateAsync(hat);

        // assert
        result.Value.Success.Should().BeFalse();
        result.Value.Errors.First().Should().Be($"{people[3].Name} is not an eligible recipient for any participant. Their name will not be picked.");
    }

    [Fact]
    public async Task Validate_GivenValidHat_ReturnsValidResult()
    {
        var people = _personFaker.Generate(4);

        // arrange
        var hat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            Status = HatStatus.InProgress,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = Persons.Empty,
            Participants = [
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[0],
                    EligibleRecipients = [ people[1].Name, people[2].Name ]
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[1],
                    EligibleRecipients = [ people[0].Name, people[2].Name ]
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[2],
                    EligibleRecipients = [ people[0].Name, people[3].Name ]
                },
                new Participant
                {
                    PickedRecipient = string.Empty,
                    Person = people[3],
                    EligibleRecipients = [ people[1].Name, people[2].Name ]
                }
            ],
            RecipientsAssigned = false,
            InvitationsQueued = false,
            InvitationsQueuedDate = DateTimeOffset.MinValue
        };

        // act
        var result = await _sut.ValidateAsync(hat);

        // assert
        result.Value.Success.Should().BeTrue();
    }
}

