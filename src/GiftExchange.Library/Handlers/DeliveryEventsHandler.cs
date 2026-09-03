using Amazon.Lambda.SQSEvents;

using AWS.Lambda.Powertools.Tracing;

namespace GiftExchange.Library.Handlers;

/// <summary>
/// Entry point for SES event notifications about participant email.
///
/// SES publishes to SNS, SNS delivers to the queue this drains. The hop through a queue is what
/// makes a DSQL write safe to attempt here: SNS retries a failed HTTP delivery and then gives up,
/// whereas a message left on a queue is still there to be tried again.
/// </summary>
public class DeliveryEventsHandler
{
    private IServiceProvider? _serviceProvider;
    private readonly Lock _serviceProviderLock = new();

    public DeliveryEventsHandler() { }

    protected DeliveryEventsHandler(IServiceProvider serviceProvider)
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

    [UsedImplicitly]
    [Tracing(CaptureMode = TracingCaptureMode.Error)]
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var service = GetServiceProvider().GetService<DeliveryEventsService>()!;

        // Anything thrown here fails the record and SQS redelivers it, which is what should happen
        // when DSQL is unreachable. The event source mapping takes one record at a time, so a
        // message that can never succeed takes only itself to the dead letter queue.
        foreach (var record in sqsEvent.Records)
            await service.ProcessRecordAsync(record).ConfigureAwait(false);
    }
}
