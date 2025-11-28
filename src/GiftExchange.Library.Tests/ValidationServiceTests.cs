namespace GiftExchange.Library.Tests;

public class ValidationServiceTests
{
    private readonly PersonFaker _personFaker;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public ValidationServiceTests()
    {
        _personFaker = new PersonFaker();
    }

    [Fact]
    public void Validate_GivenHatWithTooFewParticipants_ReturnsInvalidResult()
    {
        // arrange
        var hat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
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
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        // act
        var result = ValidationService.Validate(hat).Value;

        // assert
        result.Success.Should().BeFalse();
        result.Errors.First().Should().Be("A gift exchange of this type needs at least three people.");
    }

    [Fact]
    public void Validate_GivenParticipantWithNoEligibleParticipants_ReturnsInvalidResult()
    {
        var people = _personFaker.Generate(3);

        // arrange
        var hat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
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
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        // act
        var result = ValidationService.Validate(hat).Value;

        // assert
        result.Success.Should().BeFalse();
        result.Errors.First().Should().Be($"{people[2].Name} has no eligible recipients");
    }

    [Fact]
    public void Validate_GivenParticipantWhoIsNotAnEligibleRecipient_ReturnsInvalidResult()
    {
        var people = _personFaker.Generate(4);

        // arrange
        var hat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
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
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        // act
        var result = ValidationService.Validate(hat).Value;

        // assert
        result.Success.Should().BeFalse();
        result.Errors.First().Should().Be($"{people[3].Name} is not an eligible recipient for any participant. Their name will not be picked.");
    }

    [Fact]
    public void Validate_GivenValidHat_ReturnsValidResult()
    {
        var people = _personFaker.Generate(4);

        // arrange
        var hat = new Hat
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
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
            OrganizerVerified = false,
            RecipientsAssigned = false
        };

        // act
        var result = ValidationService.Validate(hat).Value;

        // assert
        result.Success.Should().BeTrue();
    }
}

