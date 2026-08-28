namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// The email that goes out once the organizer says the exchange has happened. It is the only one
/// that names every pick, so what it says about them is worth pinning down.
/// </summary>
public class CompletionEmailCompositionServiceTests
{
    private readonly CompletionEmailCompositionService _sut = new();

    [Theory]
    [InlineData("Family Christmas", "The gift exchange, Family Christmas, has finished")]
    // The same reason the invitation subject reads the way it does: the sentence never leans on
    // the organizer's name for the exchange, so any name slots in.
    [InlineData("Christmas On August 27", "The gift exchange, Christmas On August 27, has finished")]
    public void GetSubject_NamesTheExchangeWithoutBuildingTheSentenceAroundItsName(
        string hatName,
        string expected)
    {
        // act
        var subject = CompletionEmailCompositionService.GetSubject(HatFor(hatName));

        // assert
        subject.Should().Be(expected);
    }

    [Fact]
    public void GetSubject_GivenNoName_StillReads()
    {
        // act: the validators require a name, so this is the defensive branch rather than a shape
        // anything sends today.
        var subject = CompletionEmailCompositionService.GetSubject(HatFor(string.Empty));

        // assert
        subject.Should().Be("The gift exchange has finished");
    }

    [Fact]
    public void ComposeEmail_SaysTheOrganizerCalledTheExchangeOver()
    {
        // act
        var body = _sut.ComposeEmail(HatFor("Family Christmas"), "Alice");

        // assert
        body.Should().Contain("Dear Alice,");
        body.Should().Contain("Ben (ben@example.com) has let us know that the gift exchange, Family Christmas, is over.");
        body.Should().Contain("We hope everybody came away with something they liked.");
    }

    [Fact]
    public void ComposeEmail_CarriesTheWholeDrawForTheRecord()
    {
        // act
        var body = _sut.ComposeEmail(HatFor("Family Christmas"), "Alice");

        // assert: every pairing, not only the reader's own. Nothing is secret once the organizer
        // has revealed the picks, and the list is the record participants are being sent.
        body.Should().Contain("Here's who picked whose name, for the record:");
        body.Should().Contain("Alice &rarr;").And.Contain("<b>Bob</b>");
        body.Should().Contain("Bob &rarr;").And.Contain("<b>Charlie</b>");
        body.Should().Contain("Charlie &rarr;").And.Contain("<b>Alice</b>");
    }

    [Fact]
    public void ComposeEmail_NamesNobodyWhoWasNotDrawn()
    {
        // arrange: a participant with no pick recorded cannot happen from the close path, which
        // only accepts a hat that has been shaken. Half a row is worse than no row.
        var hat = HatFor("Family Christmas") with
        {
            Participants = [ParticipantFor("Alice", "alice@example.com", string.Empty)]
        };

        // act
        var body = _sut.ComposeEmail(hat, "Alice");

        // assert
        body.Should().NotContain("for the record");
        body.Should().NotContain("&rarr;");

        // The rest of the email still goes: they are owed the news that it is over either way.
        body.Should().Contain("is over.");
    }

    [Fact]
    public void ComposeEmail_EncodesEveryValueItPlacesIntoTheHtml()
    {
        // arrange: the validators reject angle brackets in names, but this body is assembled by
        // string concatenation and should not depend on that holding.
        var hat = HatFor("Ampersand & Co") with
        {
            Organizer = new Person { Name = "<script>alert(1)</script>", Email = "evil\"@example.com" },
            Participants = [ParticipantFor("<script>a</script>", "a@example.com", "<img src=x onerror=alert(1)>")]
        };

        // act
        var body = _sut.ComposeEmail(hat, "<script>b</script>");

        // assert
        body.Should().NotContain("<script>");
        body.Should().Contain("&lt;script&gt;");
        body.Should().Contain("&lt;img src=x onerror=alert(1)&gt;");

        // Stated as a count rather than as an absence: the masthead is a real image, so an
        // injected tag would open a second one.
        body.Split("<img").Length.Should().Be(
            2,
            "the branding masthead is the only <img> this email is allowed to carry");
    }

    [Fact]
    public void ComposeEmail_CarriesTheBrandingAndSaysWhereItCameFrom()
    {
        // act
        var body = _sut.ComposeEmail(HatFor("Family Christmas"), "Alice");

        // assert
        body.Should().StartWith("<a href=\"https://namesoutofahat.com\"><img");
        body.Should().Contain("This email was sent on behalf of ben@example.com");

        // Nothing reads a reply to the sending address, so the way to reach a human is the
        // organizer, and the email has to say so.
        body.Should().Contain("mailto:ben@example.com");
        body.Should().Contain("Nobody reads replies to this address");
    }

    [Fact]
    public void ComposeEmail_KeepsTheGiftIdeasMachineryOutOfIt()
    {
        // act
        var body = _sut.ComposeEmail(HatFor("Family Christmas"), "Alice");

        // assert: the exchange is over, so an invitation to share ideas would be answered by a
        // rejection. Nothing here should offer it.
        body.Should().NotContain("SHARE GIFT IDEAS");
        body.Should().NotContain("ideas.namesoutofahat.com");
    }

    private static Hat HatFor(string hatName) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Name = hatName,
            Status = HatStatus.CooledOff,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = new Person { Name = "Ben", Email = "ben@example.com" },
            Participants =
            [
                ParticipantFor("Alice", "alice@example.com", "Bob"),
                ParticipantFor("Bob", "bob@example.com", "Charlie"),
                ParticipantFor("Charlie", "charlie@example.com", "Alice")
            ],
            InvitationsQueuedDate = DateTimeOffset.MinValue
        };

    private static Participant ParticipantFor(string name, string email, string pickedRecipient) =>
        new()
        {
            Person = new Person { Name = name, Email = email },
            PickedRecipient = pickedRecipient,
            EligibleRecipients = []
        };
}
