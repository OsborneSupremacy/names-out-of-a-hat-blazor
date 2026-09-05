using Amazon.Scheduler;
using Amazon.Scheduler.Model;

namespace GiftExchange.Library.Services;

internal class SchedulerService : ISchedulerService
{
    /// <summary>
    /// How long after invitations are queued the delivery check runs.
    /// </summary>
    /// <remarks>
    /// Two hours, and the number is a compromise between two ways of being wrong.
    ///
    /// Too soon and the notice is incomplete: SES publishes a bounce when it stops retrying rather
    /// than when the first attempt fails, and a receiving server that is briefly unavailable can be
    /// retried for the better part of an hour before either outcome is known. An organizer told
    /// about half the failures learns to distrust the email.
    ///
    /// Too late and it is useless. The point of this is to reach an organizer while sending
    /// invitations is still the thing they are doing, not the day after, and a participant who has
    /// not heard anything by the evening has already asked somebody about it.
    ///
    /// A constant rather than a setting, because there is no environment where a different value
    /// would be right, and because a schedule already sitting in EventBridge would not pick up a
    /// change to one anyway.
    /// </remarks>
    private static readonly TimeSpan UndeliverableCheckDelay = TimeSpan.FromHours(2);

    private readonly ILogger<SchedulerService> _logger;

    private readonly JsonService _jsonService;

    private readonly IAmazonScheduler _schedulerClient;

    private readonly string _cooledOffSchedulerTargetArn;

    private readonly string _cooledOffSchedulerRoleArn;

    private readonly string _cooledOffSchedulerGroupName;

    private readonly string _undeliverableSchedulerTargetArn;

    private readonly string _undeliverableSchedulerRoleArn;

    private readonly string _undeliverableSchedulerGroupName;

    public SchedulerService(
        ILogger<SchedulerService> logger,
        JsonService jsonService,
        IAmazonScheduler schedulerClient
    )
    {
        _logger =  logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonService = jsonService ?? throw new ArgumentNullException(nameof(jsonService));
        _schedulerClient = schedulerClient ?? throw new ArgumentNullException(nameof(schedulerClient));
        _cooledOffSchedulerTargetArn = EnvReader.GetStringValue("COOLED_OFF_SCHEDULER_TARGET_ARN");
        _cooledOffSchedulerRoleArn = EnvReader.GetStringValue("COOLED_OFF_SCHEDULER_ROLE_ARN");
        _cooledOffSchedulerGroupName = EnvReader.GetStringValue("COOLED_OFF_SCHEDULER_GROUP_NAME");
        _undeliverableSchedulerTargetArn = EnvReader.GetStringValue("UNDELIVERABLE_SCHEDULER_TARGET_ARN");
        _undeliverableSchedulerRoleArn = EnvReader.GetStringValue("UNDELIVERABLE_SCHEDULER_ROLE_ARN");
        _undeliverableSchedulerGroupName = EnvReader.GetStringValue("UNDELIVERABLE_SCHEDULER_GROUP_NAME");
    }

    public Task CreateCooledOffScheduleAsync(
        SendInvitationsRequest request,
        DateTimeOffset invitationsQueuedAt
        ) =>
        CreateScheduleAsync(
            $"hat-cooled-off-{request.HatId:N}",
            _cooledOffSchedulerGroupName,
            invitationsQueuedAt.AddMinutes(5),
            _cooledOffSchedulerTargetArn,
            _cooledOffSchedulerRoleArn,
            _jsonService.SerializeDefault(new HatCooledOffScheduleRequest
            {
                OrganizerEmail = request.OrganizerEmail,
                HatId = request.HatId
            }));

    public Task CreateUndeliverableInvitationsScheduleAsync(
        SendInvitationsRequest request,
        DateTimeOffset invitationsQueuedAt
        ) =>
        CreateScheduleAsync(
            $"hat-undeliverable-{request.HatId:N}",
            _undeliverableSchedulerGroupName,
            invitationsQueuedAt.Add(UndeliverableCheckDelay),
            _undeliverableSchedulerTargetArn,
            _undeliverableSchedulerRoleArn,
            _jsonService.SerializeDefault(new UndeliverableInvitationsScheduleRequest
            {
                OrganizerEmail = request.OrganizerEmail,
                HatId = request.HatId
            }));

    /// <summary>
    /// Creates one one-shot schedule, and never throws.
    /// </summary>
    /// <remarks>
    /// Failures are logged and swallowed for the reason the cool-off schedule has always swallowed
    /// them: by the time anything here runs the invitations are already on the queue, and throwing
    /// would surface as a failed send and invite a retry that mails everybody twice. What is lost
    /// when this fails is a hat that never becomes closable, or a delivery notice that never
    /// arrives -- both recoverable, unlike a second round of invitations.
    ///
    /// The name is derived from the hat, so a second send inside the window of a schedule that has
    /// not fired yet collides rather than queueing a duplicate. That collision lands here as a
    /// swallowed ConflictException, which is the correct outcome: the schedule already waiting will
    /// do the job.
    /// </remarks>
    private async Task CreateScheduleAsync(
        string scheduleName,
        string groupName,
        DateTimeOffset firesAt,
        string targetArn,
        string roleArn,
        string payload
    )
    {
        var createRequest = new CreateScheduleRequest
        {
            Name = scheduleName,
            GroupName = groupName,
            ActionAfterCompletion = ActionAfterCompletion.DELETE,
            // OFF, not FLEXIBLE. FLEXIBLE requires MaximumWindowInMinutes and EventBridge rejects
            // the request without it, which is why no schedule was ever created and hats stayed at
            // INVITATIONS_SENT forever.
            //
            // A flexible window would also be actively unhelpful: it lets the schedule fire any
            // time inside the window, so a short cool-off could be overshot by more than the
            // cool-off itself. Nothing here needs the load-spreading it exists for.
            FlexibleTimeWindow = new FlexibleTimeWindow
            {
                Mode = FlexibleTimeWindowMode.OFF
            },
            ScheduleExpression = $"at({firesAt.UtcDateTime:yyyy-MM-ddTHH:mm:ss})",
            Target = new Target
            {
                Arn = targetArn,
                RoleArn = roleArn,
                Input = payload
            }
        };

        try
        {
            await _schedulerClient
                .CreateScheduleAsync(createRequest)
                .ConfigureAwait(false);
        } catch (Exception exception)
        {
            _logger
                .LogError(
                    exception,
                    "Failed to create schedule. Will not retry since we don't want to risk sending multiple invitations. Exception: {Exception}, ScheduleName: {ScheduleName}, GroupName: {GroupName}",
                    exception,
                    scheduleName,
                    groupName
                );
        };
    }
}
