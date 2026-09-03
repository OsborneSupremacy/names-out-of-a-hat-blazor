using Amazon.Lambda.SQSEvents;

using AWS.Lambda.Powertools.Tracing;

namespace GiftExchange.Library.Handlers;

public class InvitationQueueHandler
{
    private IServiceProvider? _serviceProvider;
    private readonly Lock _serviceProviderLock = new();

    public InvitationQueueHandler() { }

    protected InvitationQueueHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private IServiceProvider GetServiceProvider()
    {
        if(_serviceProvider is not null) return _serviceProvider;
        using (_serviceProviderLock.EnterScope())
        {
            if(_serviceProvider is not null) return _serviceProvider;
            _serviceProvider = ServiceProviderBuilder.Build();
        }
        return _serviceProvider;
    }

    [UsedImplicitly]
    // The far end of the trace EmailQueue propagates. With AWSTraceHeader on the message, this
    // subsegment attaches to the enqueue that produced it rather than starting a trace of its own.
    //
    // Error capture only: this returns nothing, but an exception here is raised while holding a
    // participant's address, so the argument is the same one made on the router.
    [Tracing(CaptureMode = TracingCaptureMode.Error)]
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var service = GetServiceProvider().GetService<InvitationQueueHandlerService>()!;

        foreach (var record in sqsEvent.Records)
            await service.ProcessRecordAsync(record, context)
                .ConfigureAwait(false);
    }
}
