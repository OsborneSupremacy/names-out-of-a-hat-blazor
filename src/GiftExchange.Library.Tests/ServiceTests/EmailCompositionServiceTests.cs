namespace GiftExchange.Library.Tests.ServiceTests;

public class EmailCompositionServiceTests
{
    private readonly EmailCompositionService _sut = new();

    [Fact]
    public void ComposeEmail_NamesTheOrganizersAddressAlongsideTheirName()
    {
        // act
        var body = _sut.ComposeEmail(HatFor("Ben", "ben@example.com", "Family Christmas"), "Alice", "Charlie", "token");

        // assert
        body.Should().Contain("Ben (ben@example.com) has added you to the gift exchange, Family Christmas!");
    }

    [Theory]
    // A name that does read as a noun phrase, which is the easy case.
    [InlineData("Family Christmas", "Ben has added you to the gift exchange, Family Christmas!")]
    // And one that does not. "added you to the Christmas On August 27!" was the version of this
    // line that made the phrasing worth changing.
    [InlineData("Christmas On August 27", "Ben has added you to the gift exchange, Christmas On August 27!")]
    // A name carrying its own article no longer needs handling of its own: nothing is prepended to
    // the name at all, so there is no second article to avoid.
    [InlineData("The Osborne Exchange", "Ben has added you to the gift exchange, The Osborne Exchange!")]
    public void GetSubject_NamesTheExchangeWithoutBuildingTheSentenceAroundItsName(
        string hatName,
        string expected)
    {
        // act
        var subject = EmailCompositionService.GetSubject(HatFor("Ben", "ben@example.com", hatName));

        // assert
        subject.Should().Be(expected);
    }

    [Fact]
    public void GetSubject_GivenNoName_StillReads()
    {
        // act
        var subject = EmailCompositionService.GetSubject(HatFor("Ben", "ben@example.com", string.Empty));

        // assert: the validators require a name, so this is the defensive branch rather than a
        // shape anything sends today.
        subject.Should().Be("Ben has added you to the gift exchange!");
    }

    [Fact]
    public void ComposeEmail_CarriesTheSmallPrintAndTheOrganizersAddress()
    {
        // act
        var body = _sut.ComposeEmail(HatFor("Ben", "ben@example.com", "Family Christmas"), "Alice", "Charlie", "token");

        // assert
        body.Should().Contain("This email was sent on behalf of ben@example.com");
        body.Should().Contain("namesoutofahat.com is not responsible for them");
        body.Should().Contain("namesoutofahat.com");
        body.Should().NotContain("namesoutofhat.com", "the domain is namesoutofahat.com");
    }

    [Fact]
    public void ComposeEmail_EncodesEveryValueItPlacesIntoTheHtml()
    {
        // arrange: the validators reject angle brackets in names, but this body is assembled by
        // string concatenation and should not depend on that holding.
        var hat = HatFor("<script>alert(1)</script>", "evil\"@example.com", "Ampersand & Co")
            with { PriceRange = "<b>$20</b>", AdditionalInformation = "<img src=x onerror=alert(1)>" };

        // act
        var body = _sut.ComposeEmail(hat, "<script>a</script>", "<script>b</script>", "token");

        // assert
        // What makes the payloads inert is the escaped angle brackets, not the absence of the
        // words: "onerror=" survives as harmless text once "<img" cannot open a tag.
        body.Should().NotContain("<script>");
        body.Should().NotContain("<img");
        body.Should().Contain("&lt;script&gt;");
        body.Should().Contain("&lt;img src=x onerror=alert(1)&gt;");
        body.Should().Contain("Ampersand &amp; Co");
    }

    [Fact]
    public void ComposeEmail_CarriesAGiftIdeasButtonAddressedToTheParticipantsOwnToken()
    {
        // act
        var body = _sut.ComposeEmail(
            HatFor("Ben", "ben@example.com", "Family Christmas"),
            "Alice",
            "Charlie",
            "abc123");

        // assert
        body.Should().Contain("SHARE GIFT IDEAS");
        body.Should().Contain("mailto:abc123@ideas.namesoutofahat.com");

        // Printed in full as well as linked. A client that is not registered as the handler for
        // mailto: does nothing at all when the button is clicked, with no error to explain it.
        body.Should().Contain("abc123@ideas.namesoutofahat.com");
    }

    [Fact]
    public void ComposeEmail_GivesTheGiftIdeasButtonAMailtoRatherThanAReply()
    {
        // act
        var body = _sut.ComposeEmail(
            HatFor("Ben", "ben@example.com", "Family Christmas"),
            "Alice",
            "Charlie",
            "abc123");

        // assert: this is a security property, not a styling one. This email names the recipient's
        // own pick, so a reply would quote it, and the quoted text would be forwarded to the one
        // person who must never learn it. A mailto: opens an empty message with nothing to quote.
        body.Should().NotContain("href=\"mailto:donotreply");
        body.Should().Contain("Please do not reply to this email",
            "the warning is what steers somebody away from the reply button and towards the button");
    }

    [Fact]
    public void ComposeEmail_GivenNoToken_LeavesTheGiftIdeasBlockOutEntirely()
    {
        // act: an invitation with no token issued gets no block, rather than a button addressed to
        // "@ideas.namesoutofahat.com" that silently routes nowhere.
        var body = _sut.ComposeEmail(
            HatFor("Ben", "ben@example.com", "Family Christmas"),
            "Alice",
            "Charlie",
            string.Empty);

        // assert
        body.Should().NotContain("SHARE GIFT IDEAS");
        body.Should().NotContain("ideas.namesoutofahat.com");
    }

    private static Hat HatFor(string organizerName, string organizerEmail, string hatName) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Name = hatName,
            Status = HatStatus.NamesAssigned,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Organizer = new Person { Name = organizerName, Email = organizerEmail },
            Participants = [],
            InvitationsQueuedDate = DateTimeOffset.MinValue
        };
}
