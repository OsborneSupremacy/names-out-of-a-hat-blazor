using Amazon.Scheduler;
using Amazon.Scheduler.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// The schedule this builds is what eventually lets an exchange be closed. Creation failures are
/// swallowed on purpose, so a malformed request produces no error anywhere the organizer can see —
/// it just means the hat is never closable. That makes the shape of the request worth asserting.
/// </summary>
public class SchedulerServiceTests
{
    private readonly IAmazonScheduler _scheduler = Substitute.For<IAmazonScheduler>();

    private readonly ISchedulerService _sut;

    public SchedulerServiceTests()
    {
        DotEnv.Load();

        var serviceProvider = new ServiceCollection().AddUtilities().BuildServiceProvider();

        _sut = new SchedulerService(
            Substitute.For<ILogger<SchedulerService>>(),
            serviceProvider.GetRequiredService<JsonService>(),
            _scheduler);
    }

    [Fact]
    public async Task CreateCooledOffScheduleAsync_BuildsARequestEventBridgeWillAccept()
    {
        // act
        await CreateScheduleAsync();

        // assert: EventBridge rejects FLEXIBLE without MaximumWindowInMinutes, which is exactly
        // how this silently failed in production.
        var request = CapturedRequest();

        if (request.FlexibleTimeWindow.Mode == FlexibleTimeWindowMode.FLEXIBLE)
            request.FlexibleTimeWindow.MaximumWindowInMinutes
                .Should().NotBeNull("EventBridge requires a window size when the mode is FLEXIBLE");
        else
            request.FlexibleTimeWindow.Mode.Should().Be(FlexibleTimeWindowMode.OFF);
    }

    [Fact]
    public async Task CreateCooledOffScheduleAsync_TargetsTheHandlerAndCarriesTheHat()
    {
        // arrange
        var hatId = Guid.CreateVersion7();

        // act
        await CreateScheduleAsync(hatId, "organizer@example.com");

        // assert
        var request = CapturedRequest();

        request.Name.Should().Be($"hat-cooled-off-{hatId:N}");
        request.ActionAfterCompletion.Should().Be(ActionAfterCompletion.DELETE);
        request.Target.Input.Should().Contain(hatId.ToString());
        request.Target.Input.Should().Contain("organizer@example.com");
    }

    [Fact]
    public async Task CreateCooledOffScheduleAsync_WhenCreationFails_DoesNotThrow()
    {
        // arrange: invitations have already been queued by this point, so throwing would surface
        // as a failed send and invite a retry that mails everybody twice. Swallowing is the
        // deliberate trade, and the reason a malformed request went unnoticed for so long.
        _scheduler
            .CreateScheduleAsync(Arg.Any<CreateScheduleRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ValidationException("nope"));

        // act
        var act = async () => await CreateScheduleAsync();

        // assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateUndeliverableInvitationsScheduleAsync_TargetsItsOwnHandlerAndCarriesTheHat()
    {
        // arrange
        var hatId = Guid.CreateVersion7();

        // act
        await _sut.CreateUndeliverableInvitationsScheduleAsync(
            Request(hatId, "organizer@example.com"),
            DateTimeOffset.UtcNow);

        // assert: its own name, its own group and its own target. Sharing any of the three with the
        // cool-off schedule would mean one send creating two schedules that collide by name, and
        // only the first of them ever being created.
        var request = CapturedRequest();

        request.Name.Should().Be($"hat-undeliverable-{hatId:N}");
        request.GroupName.Should().NotBe("test-cooled-off");
        request.Target.Arn.Should().NotBe("arn:aws:lambda:us-east-1:000000000000:function:test-target");
        request.ActionAfterCompletion.Should().Be(ActionAfterCompletion.DELETE);
        request.Target.Input.Should().Contain(hatId.ToString());
        request.Target.Input.Should().Contain("organizer@example.com");
    }

    /// <summary>
    /// The delay is the whole design of the feature: SES publishes a bounce when it stops retrying
    /// rather than when the first attempt fails, so a check run too soon reports half the failures.
    /// </summary>
    [Fact]
    public async Task CreateUndeliverableInvitationsScheduleAsync_FiresHoursAfterTheInvitationsWereQueued()
    {
        // arrange
        var queuedAt = new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.Zero);

        // act
        await _sut.CreateUndeliverableInvitationsScheduleAsync(Request(), queuedAt);

        // assert: counted from when the invitations were queued rather than from now, so that this
        // and the cool-off schedule cannot drift apart over the time the two calls take.
        CapturedRequest().ScheduleExpression.Should().Be("at(2026-12-01T11:00:00)");
    }

    [Fact]
    public async Task CreateUndeliverableInvitationsScheduleAsync_WhenCreationFails_DoesNotThrow()
    {
        // arrange: same trade as the cool-off schedule. The invitations are already on the queue,
        // so a throw here would be reported as a failed send and invite a retry that mails
        // everybody twice. What is lost instead is one notice about bad addresses, and the delivery
        // column still says everything it would have said.
        _scheduler
            .CreateScheduleAsync(Arg.Any<CreateScheduleRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConflictException("already exists"));

        // act
        var act = async () =>
            await _sut.CreateUndeliverableInvitationsScheduleAsync(Request(), DateTimeOffset.UtcNow);

        // assert
        await act.Should().NotThrowAsync();
    }

    private Task CreateScheduleAsync(Guid? hatId = null, string organizerEmail = "organizer@example.com") =>
        _sut.CreateCooledOffScheduleAsync(Request(hatId, organizerEmail), DateTimeOffset.UtcNow);

    private static SendInvitationsRequest Request(
        Guid? hatId = null,
        string organizerEmail = "organizer@example.com"
    ) =>
        new()
        {
            HatId = hatId ?? Guid.CreateVersion7(),
            OrganizerEmail = organizerEmail
        };

    private CreateScheduleRequest CapturedRequest() =>
        (CreateScheduleRequest)_scheduler
            .ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IAmazonScheduler.CreateScheduleAsync))
            .GetArguments()[0]!;
}
