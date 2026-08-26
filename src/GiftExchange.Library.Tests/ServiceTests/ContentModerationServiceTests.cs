using Amazon.Comprehend;
using Microsoft.Extensions.Logging;
using Amazon.Comprehend.Model;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// Everything this service approves ends up in an email sent from our SES identity, so it fails
/// closed. These tests exist mostly to stop that quietly reverting.
/// </summary>
public class ContentModerationServiceTests
{
    private readonly IAmazonComprehend _comprehend = Substitute.For<IAmazonComprehend>();

    private readonly IContentModerationService _sut;

    public ContentModerationServiceTests()
    {
        DotEnv.Load();
        _sut = new ContentModerationService(_comprehend, Substitute.For<ILogger<ContentModerationService>>());
    }

    [Fact]
    public async Task ValidateContentAsync_GivenCleanContent_IsValid()
    {
        // arrange
        RespondWithScore(0.01f);

        // act
        var (isValid, error) = await _sut.ValidateContentAsync("Family Christmas", "gift exchange name");

        // assert
        isValid.Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateContentAsync_GivenContentOverTheThreshold_IsRejected()
    {
        // arrange
        RespondWithScore(0.99f);

        // act
        var (isValid, error) = await _sut.ValidateContentAsync("something nasty", "gift exchange name");

        // assert
        isValid.Should().BeFalse();
        error.Should().Contain("inappropriate content");
    }

    [Fact]
    public async Task ValidateContentAsync_WhenComprehendThrows_RejectsRatherThanAccepting()
    {
        // arrange
        _comprehend
            .DetectToxicContentAsync(Arg.Any<DetectToxicContentRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonComprehendException("Comprehend is unavailable"));

        // act
        var (isValid, error) = await _sut.ValidateContentAsync("Family Christmas", "gift exchange name");

        // assert: unchecked content must not reach an email sent from our SES identity.
        isValid.Should().BeFalse();

        // The content may be perfectly fine, so the message has to read as "try again" rather than
        // accusing the user of writing something offensive.
        error.Should().NotContain("inappropriate");
        error.Should().Contain("try again");
    }

    [Fact]
    public async Task ValidateMultipleFieldsAsync_WhenComprehendThrows_RejectsEveryField()
    {
        // arrange
        _comprehend
            .DetectToxicContentAsync(Arg.Any<DetectToxicContentRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonComprehendException("Comprehend is unavailable"));

        // act
        var (isValid, errors) = await _sut.ValidateMultipleFieldsAsync(new Dictionary<string, string>
        {
            ["gift exchange name"] = "Family Christmas",
            ["organizer name"] = "Ben"
        });

        // assert
        isValid.Should().BeFalse();
        errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateContentAsync_GivenEmptyText_IsValidWithoutCallingComprehend()
    {
        // act: optional fields arrive empty, and there is nothing to check.
        var (isValid, _) = await _sut.ValidateContentAsync(string.Empty, "price range");

        // assert
        isValid.Should().BeTrue();
        await _comprehend
            .DidNotReceive()
            .DetectToxicContentAsync(Arg.Any<DetectToxicContentRequest>(), Arg.Any<CancellationToken>());
    }

    private void RespondWithScore(float score) =>
        _comprehend
            .DetectToxicContentAsync(Arg.Any<DetectToxicContentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DetectToxicContentResponse
            {
                ResultList =
                [
                    new ToxicLabels
                    {
                        Labels = [new ToxicContent { Name = ToxicContentType.PROFANITY, Score = score }]
                    }
                ]
            });
}
