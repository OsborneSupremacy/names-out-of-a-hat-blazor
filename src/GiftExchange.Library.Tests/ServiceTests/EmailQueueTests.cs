using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.XRay.Recorder.Core;
using GiftExchange.Library.Utility;
using NSubstitute;

namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// Putting a participant email onto the queue.
///
/// Almost all of this class is one SendMessage call, and the part worth testing is the trace header
/// it attaches — which is the part nothing tested before, because it is written only when there is a
/// live X-Ray entity to read. Outside Lambda there never is, so every test that had touched this
/// path took the other branch, and a NullReferenceException in the branch they skipped reached
/// production and broke every send.
///
/// So these tests start a real segment rather than substituting the trace away. That is what makes
/// them able to fail.
/// </summary>
[Collection(TracingCollection.Name)]
public class EmailQueueTests
{
    static EmailQueueTests()
    {
        DotEnv.Load();
        Environment.SetEnvironmentVariable("INVITATIONS_QUEUE_URL", "https://sqs.test/queue");
    }

    private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();

    private readonly List<SendMessageRequest> _sent = [];

    private readonly EmailQueue _sut;

    public EmailQueueTests()
    {
        // The real JsonService, from the real registration: what goes onto the queue has to be what
        // the handler at the other end reads back, and a hand-built serializer here would not be
        // the one production uses.
        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .BuildServiceProvider();

        _sqs.SendMessageAsync(Arg.Do<SendMessageRequest>(request => _sent.Add(request)), Arg.Any<CancellationToken>())
            .Returns(new SendMessageResponse());

        _sut = new EmailQueue(_sqs, serviceProvider.GetRequiredService<JsonService>());
    }

    [Fact]
    public async Task InsideATrace_CarriesTheTraceHeaderOntoTheMessage()
    {
        // arrange: a real segment, because the header is read off the recorder and there is no
        // seam to substitute. Without one this test passes on the branch that was never broken.
        AWSXRayRecorder.Instance.BeginSegment("EmailQueueTests");

        try
        {
            // act
            await _sut.EnqueueAsync(AnInvitation());
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSegment();
        }

        // assert: the regression. In AWS SDK for .NET v4 this collection starts null, so writing
        // through the indexer threw here and took the whole request down with it.
        var request = _sent.Should().ContainSingle().Subject;

        request.MessageSystemAttributes.Should().ContainKey("AWSTraceHeader");
        request.MessageSystemAttributes["AWSTraceHeader"].StringValue.Should().StartWith("Root=");
        request.MessageSystemAttributes["AWSTraceHeader"].DataType.Should().Be("String");
    }

    [Fact]
    public async Task OutsideATrace_SendsTheMessageWithNoHeaderRatherThanFailing()
    {
        // act: the ordinary state outside Lambda. Tracing that can fail a send would be worse than
        // tracing that records nothing.
        await _sut.EnqueueAsync(AnInvitation());

        // assert
        var request = _sent.Should().ContainSingle().Subject;

        request.QueueUrl.Should().Be("https://sqs.test/queue");
        request.MessageBody.Should().Contain("alice@example.com");
    }

    private static GiftExchangeEmailRequest AnInvitation() =>
        new()
        {
            HatId = Guid.CreateVersion7(),
            OrganizerEmail = "ben@example.com",
            RecipientEmail = "alice@example.com",
            ParticipantId = Guid.CreateVersion7(),
            MessageType = EmailMessageType.Invitation,
            Subject = "You've been added",
            HtmlBody = "<p>hello</p>"
        };
}

/// <summary>
/// Tests that begin an X-Ray segment, kept off the same thread pool at the same time as each other.
/// </summary>
/// <remarks>
/// <c>AWSXRayRecorder.Instance</c> is a process-wide singleton holding one trace context, so two
/// tests starting segments concurrently would each see the other's entity. Collections are xUnit's
/// way of saying "not in parallel with these".
/// </remarks>
[CollectionDefinition(Name)]
public class TracingCollection
{
    public const string Name = "tracing";
}
