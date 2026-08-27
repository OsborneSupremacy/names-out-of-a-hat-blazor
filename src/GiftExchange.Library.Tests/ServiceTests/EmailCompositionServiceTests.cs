namespace GiftExchange.Library.Tests.ServiceTests;

public class EmailCompositionServiceTests
{
    private readonly EmailCompositionService _sut = new();

    [Fact]
    public void ComposeEmail_NamesTheOrganizersAddressAlongsideTheirName()
    {
        // act
        var body = _sut.ComposeEmail(HatFor("Ben", "ben@example.com", "Family Christmas"), "Alice", "Charlie");

        // assert
        body.Should().Contain("Ben (ben@example.com) has added you to the Family Christmas!");
    }

    [Fact]
    public void GetSubject_DoesNotDoubleTheArticle()
    {
        // act
        var subject = EmailCompositionService.GetSubject(HatFor("Ben", "ben@example.com", "Family Christmas"));

        // assert: this read "added you to the the Family Christmas!" before.
        subject.Should().Be("Ben has added you to the Family Christmas!");
    }

    [Fact]
    public void GetSubject_DoesNotAddASecondArticleToANameThatHasOne()
    {
        // act
        var subject = EmailCompositionService.GetSubject(HatFor("Ben", "ben@example.com", "The Osborne Exchange"));

        // assert
        subject.Should().Be("Ben has added you to The Osborne Exchange!");
    }

    [Fact]
    public void ComposeEmail_CarriesTheSmallPrintAndTheOrganizersAddress()
    {
        // act
        var body = _sut.ComposeEmail(HatFor("Ben", "ben@example.com", "Family Christmas"), "Alice", "Charlie");

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
        var body = _sut.ComposeEmail(hat, "<script>a</script>", "<script>b</script>");

        // assert
        // What makes the payloads inert is the escaped angle brackets, not the absence of the
        // words: "onerror=" survives as harmless text once "<img" cannot open a tag.
        body.Should().NotContain("<script>");
        body.Should().NotContain("<img");
        body.Should().Contain("&lt;script&gt;");
        body.Should().Contain("&lt;img src=x onerror=alert(1)&gt;");
        body.Should().Contain("Ampersand &amp; Co");
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
