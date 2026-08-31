using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using GiftExchange.Library.Utility;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// The contact form's one job is that a message reaches a mailbox. Everything asserted here is a
/// way that quietly fails to happen: a publish SNS rejects, or a success reported to somebody
/// whose message went nowhere.
/// </summary>
public class SubmitFeedbackServiceTests
{
    private readonly IAmazonSimpleNotificationService _sns = Substitute.For<IAmazonSimpleNotificationService>();

    private readonly SubmitFeedbackService _sut;

    public SubmitFeedbackServiceTests()
    {
        DotEnv.Load();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddValidators()
            .BuildServiceProvider();

        _sut = new SubmitFeedbackService(
            Substitute.For<ILogger<SubmitFeedbackService>>(),
            serviceProvider.GetRequiredService<ApiGatewayAdapter>(),
            _sns);
    }

    [Fact]
    public async Task ExecuteAsync_PublishesToTheFeedbackTopic()
    {
        // act
        var result = await _sut.ExecuteAsync(BuildRequest());

        // assert
        result.IsFaulted.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        CapturedRequest().TopicArn.Should().Be(EnvReader.GetStringValue("FEEDBACK_TOPIC_ARN"));
    }

    [Fact]
    public async Task ExecuteAsync_CarriesTheSenderAndTheMessageInTheBody()
    {
        // arrange: the subject deliberately holds neither, so if the body loses them the
        // notification arrives saying nothing about who wrote it or what they said.
        var request = BuildRequest(
            organizerEmail: "organizer@example.com",
            message: "The invitation link 404s on my phone.");

        // act
        await _sut.ExecuteAsync(request);

        // assert
        var published = CapturedRequest();
        published.Message.Should().Contain("organizer@example.com");
        published.Message.Should().Contain("The invitation link 404s on my phone.");
    }

    [Theory]
    [InlineData("QUESTION")]
    [InlineData("FEATURE_REQUEST")]
    [InlineData("OTHER_FEEDBACK")]
    public async Task ExecuteAsync_BuildsASubjectSnsWillAccept(string category)
    {
        // act
        await _sut.ExecuteAsync(BuildRequest(category: category));

        // assert: SNS rejects a subject over 100 characters, or one carrying a line break or any
        // character outside printable ASCII. A rejected publish is a message nobody ever reads.
        var subject = CapturedRequest().Subject;

        subject.Length.Should().BeLessThanOrEqualTo(100);
        subject.Should().MatchRegex(@"^[\x20-\x7E]+$");
    }

    [Fact]
    public async Task ExecuteAsync_TagsThePublishWithTheCategory()
    {
        // arrange: nothing filters on this yet. It is asserted so that splitting the categories
        // across subscriptions stays a filter policy rather than a code change.
        // act
        await _sut.ExecuteAsync(BuildRequest(category: FeedbackCategory.FeatureRequest));

        // assert
        CapturedRequest().MessageAttributes["category"].StringValue
            .Should().Be(FeedbackCategory.FeatureRequest);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPublishFails_ReportsTheFailure()
    {
        // arrange: the opposite of the magic link endpoint, which reports success regardless. There
        // is nothing to conceal here, and a form that says "thanks" over a dropped message is one
        // the sender will not think to try again.
        _sns
            .PublishAsync(Arg.Any<PublishRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonSimpleNotificationServiceException("nope"));

        // act
        var result = await _sut.ExecuteAsync(BuildRequest());

        // assert
        result.IsFaulted.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    private static SubmitFeedbackRequest BuildRequest(
        string organizerEmail = "someone@example.com",
        string? category = null,
        string message = "Hello."
    ) =>
        new()
        {
            OrganizerEmail = organizerEmail,
            Category = category ?? FeedbackCategory.Question,
            Message = message
        };

    private PublishRequest CapturedRequest() =>
        (PublishRequest)_sns.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IAmazonSimpleNotificationService.PublishAsync))
            .GetArguments()[0]!;
}
