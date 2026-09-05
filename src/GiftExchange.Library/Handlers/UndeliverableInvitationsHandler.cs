using AWS.Lambda.Powertools.Tracing;

namespace GiftExchange.Library.Handlers;

/// <summary>
/// Entry point for the delayed check on whether the invitations an organizer sent arrived.
///
/// Invoked once per send by an EventBridge schedule created when the invitations were queued, some
/// hours later. Its own function rather than a branch inside the cool-off handler, which fires
/// minutes after a send for an unrelated reason: two schedules on two different clocks, and a
/// change to either one that could move the other is a change nobody would think to check.
/// </summary>
[UsedImplicitly]
public class UndeliverableInvitationsHandler
{
    private IServiceProvider? _serviceProvider;
    private readonly Lock _serviceProviderLock = new();

    public UndeliverableInvitationsHandler() { }

    protected UndeliverableInvitationsHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private IServiceProvider GetServiceProvider()
    {
        if (_serviceProvider is not null) return _serviceProvider;
        using (_serviceProviderLock.EnterScope())
        {
            if (_serviceProvider is not null) return _serviceProvider;
            _serviceProvider = ServiceProviderBuilder.Build();
        }

        return _serviceProvider;
    }

    /// <remarks>
    /// Nothing is thrown out of here. EventBridge Scheduler retries a failed invocation, and every
    /// retry that got as far as sending would put a second copy of the same notice in the
    /// organizer's inbox -- the service reads and sends, and holds nothing that would let a repeat
    /// recognise itself. A check that silently does not happen is the better failure: the delivery
    /// column still says everything this email would have.
    /// </remarks>
    [Tracing(CaptureMode = TracingCaptureMode.Error)]
    public async Task FunctionHandler(UndeliverableInvitationsScheduleRequest request, ILambdaContext context)
    {
        var service = GetServiceProvider().GetRequiredService<UndeliverableInvitationsService>();

        try
        {
            await service.ExecuteAsync(request).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            context.Logger.LogError(
                $"Failed to check invitation delivery for hat {request.HatId}. Will not retry. Exception: {exception}");
        }
    }
}
