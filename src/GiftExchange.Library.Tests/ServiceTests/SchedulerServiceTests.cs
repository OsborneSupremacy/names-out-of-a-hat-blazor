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

    private Task CreateScheduleAsync(Guid? hatId = null, string organizerEmail = "organizer@example.com") =>
        _sut.CreateCooledOffScheduleAsync(
            new SendInvitationsRequest
            {
                HatId = hatId ?? Guid.CreateVersion7(),
                OrganizerEmail = organizerEmail
            },
            DateTimeOffset.UtcNow);

    private CreateScheduleRequest CapturedRequest() =>
        (CreateScheduleRequest)_scheduler
            .ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IAmazonScheduler.CreateScheduleAsync))
            .GetArguments()[0]!;
}
