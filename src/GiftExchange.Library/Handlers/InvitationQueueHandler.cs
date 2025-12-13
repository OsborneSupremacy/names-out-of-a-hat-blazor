using Amazon.Lambda.SQSEvents;

namespace GiftExchange.Library.Handlers;

public class InvitationQueueHandler
{
    private IServiceProvider? _serviceProvider;
    private readonly object _serviceProviderLock = new();

    protected InvitationQueueHandler() { }

    protected InvitationQueueHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private IServiceProvider GetServiceProvider()
    {
        if(_serviceProvider is not null) return _serviceProvider;
        lock (_serviceProviderLock)
        {
            if(_serviceProvider is not null) return _serviceProvider;
            _serviceProvider = ServiceProviderBuilder.Build();
        }
        return _serviceProvider;
    }

    [UsedImplicitly]
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var service = GetServiceProvider().GetService<InvitationQueueHandlerService>()!;

        foreach (var record in sqsEvent.Records)
            await service.ProcessRecordAsync(record, context)
                .ConfigureAwait(false);
    }
}
