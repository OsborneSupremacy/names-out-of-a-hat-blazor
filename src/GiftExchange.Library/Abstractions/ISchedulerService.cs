namespace GiftExchange.Library.Abstractions;

internal interface ISchedulerService
{
    public Task CreateCooledOffScheduleAsync(
        SendInvitationsRequest request,
        DateTimeOffset invitationsQueuedAt
    );

    /// <summary>
    /// Arranges for somebody to look, hours from now, at whether these invitations arrived.
    /// </summary>
    /// <remarks>
    /// Takes the same two arguments as the cool-off schedule and is created alongside it, from the
    /// same moment: both are counted from when the invitations were queued rather than from now,
    /// so the two clocks agree even if one call is made appreciably after the other.
    /// </remarks>
    public Task CreateUndeliverableInvitationsScheduleAsync(
        SendInvitationsRequest request,
        DateTimeOffset invitationsQueuedAt
    );
}
