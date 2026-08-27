using System.Text;

namespace GiftExchange.Library.Tests.ServiceTests;

public class GiftIdeaContentPolicyTests
{
    private readonly GiftIdeaContentPolicy _sut = new();

    [Fact]
    public void Check_GivenOrdinaryGiftIdeas_Passes()
    {
        // act
        var outcome = _sut.Check("A cast iron skillet, a book about bread, and warm socks.", "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.Shared);
    }

    [Fact]
    public void Check_AllowsWishlistLinks()
    {
        // act: refusing links would take most of the value out of the feature — a wishlist URL is
        // the single most useful thing somebody can send.
        var outcome = _sut.Check("My list is at https://www.amazon.com/hz/wishlist/ls/ABC123", "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.Shared);
    }

    [Theory]
    [InlineData("Have a look at https://bit.ly/3xYz1Ab")]
    [InlineData("here: tinyurl.com/my-list")]
    [InlineData("www.t.co/abcdef")]
    public void Check_RefusesShortenedLinks(string body)
    {
        // act: a shortener exists to stand in front of a destination, which defeats the one
        // mitigation doing real work here — that the recipient can see where a link goes.
        var outcome = _sut.Check(body, "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedShortenedLink);
    }

    [Fact]
    public void Check_RefusesLinksBackToThisApplication()
    {
        // act: nothing legitimate needs one, and a message that looks like it comes from us asking
        // somebody to sign in is the most valuable thing this could be made to deliver.
        var outcome = _sut.Check("Sign in here: https://namesoutofahat.com/auth?token=abc", "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedSelfReferentialLink);
    }

    [Fact]
    public void Check_RefusesAnUnreasonableNumberOfLinks()
    {
        // arrange
        var body = string.Join(
            "\n",
            Enumerable.Range(0, GiftIdeaContentPolicy.MaxLinks + 1)
                .Select(index => $"https://example.com/item/{index}"));

        // act
        var outcome = _sut.Check(body, "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedTooManyLinks);
    }

    [Fact]
    public void Check_RefusesTextNamingThePersonTheSenderDrew()
    {
        // act: the backstop behind quote stripping. Forwarding this would tell the recipient whose
        // name the sender drew, which is the one secret the application keeps.
        var outcome = _sut.Check(
            "A skillet please.\nThe person whose name was picked out of a hat for you is: Charlie",
            "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedWouldRevealTheirPick);
    }

    [Fact]
    public void Check_MatchesTheDrawnNameOnWordBoundaries()
    {
        // act: "Sam" must not fire on "same", or half the innocent messages in a hat containing a
        // Sam would be refused.
        var outcome = _sut.Check("I would like the same scarf as last year.", "Sam");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.Shared);
    }

    [Fact]
    public void Check_GivenNobodyDrawnYet_DoesNotTreatEveryMessageAsALeak()
    {
        // act: an empty name would otherwise match everywhere and refuse everything.
        var outcome = _sut.Check("A scarf, please.", string.Empty);

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.Shared);
    }

    [Fact]
    public void Check_RefusesAnEmptySubmission()
    {
        // act
        var outcome = _sut.Check("   \n  ", "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedNothingToShare);
    }

    [Fact]
    public void Check_MeasuresLengthInBytesRatherThanCharacters()
    {
        // arrange: emoji cost four UTF-8 bytes each, and a gift exchange is exactly where people
        // reach for them. A character count would pass text that moderation then refuses to look
        // at, since its own limit is measured in bytes.
        var body = string.Concat(Enumerable.Repeat("🎁", GiftIdeaContentPolicy.MaxBodyBytes / 4 + 1));

        // assert on the arrangement itself: comfortably under the cap by characters, over it by
        // bytes, which is the case that matters.
        body.Length.Should().BeLessThan(GiftIdeaContentPolicy.MaxBodyBytes);
        Encoding.UTF8.GetByteCount(body).Should().BeGreaterThan(GiftIdeaContentPolicy.MaxBodyBytes);

        // act
        var outcome = _sut.Check(body, "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedTooLong);
    }

    [Fact]
    public void Check_ReportsTheLeakBeforeComplainingAboutLinks()
    {
        // act: the ordering is deliberate. A message that both quotes an invitation and contains a
        // shortened link is a secrecy problem first — telling the sender to fix their link would
        // invite them to send the leak again.
        var outcome = _sut.Check("See https://bit.ly/abc — picked for you is: Charlie", "Charlie");

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedWouldRevealTheirPick);
    }

    [Theory]
    // "www." is stripped: it is not part of who a host is, and keeping it would mean every rule
    // had to name both spellings and would silently miss whichever was forgotten.
    [InlineData("https://www.amazon.com/dp/B01N5IB20Q", "amazon.com")]
    [InlineData("visit www.etsy.com/listing/123", "etsy.com")]
    [InlineData("https://example.com/thing.", "example.com")]
    [InlineData("(https://example.org/a)", "example.org")]
    // No scheme and no "www.", which is how a shortened link is almost always pasted.
    [InlineData("here: tinyurl.com/my-list", "tinyurl.com")]
    public void FindLinks_ReadsTheHostAndDropsTrailingPunctuation(string body, string expected)
    {
        // act
        var links = GiftIdeaContentPolicy.FindLinks(body);

        // assert: a trailing full stop or bracket belongs to the sentence, not the address.
        links.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Theory]
    [InlineData("I use Node.js and want a book about it")]
    [InlineData("The 3.5/5 star one, not the cheap version")]
    [InlineData("Anything, e.g. socks")]
    public void FindLinks_DoesNotReadOrdinaryProseAsAnAddress(string body)
    {
        // act: the bare-host arm of the pattern needs an alphabetic top-level domain and a slash
        // after it, which is what keeps these three out.
        var links = GiftIdeaContentPolicy.FindLinks(body);

        // assert
        links.Should().BeEmpty();
    }

    [Fact]
    public void FindLinks_GivenNoLinks_FindsNone()
    {
        // act
        var links = GiftIdeaContentPolicy.FindLinks("Just a warm scarf, nothing fancy.");

        // assert
        links.Should().BeEmpty();
    }
}
