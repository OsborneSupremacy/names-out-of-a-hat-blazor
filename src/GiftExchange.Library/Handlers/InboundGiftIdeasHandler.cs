using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;

using AWS.Lambda.Powertools.Tracing;

namespace GiftExchange.Library.Handlers;

/// <summary>
/// Entry point for mail arriving through SES.
///
/// The receipt rule writes the message to S3 and then invokes this, so what arrives here is
/// headers and verdicts; the body is fetched from the bucket by the service. That split is SES's,
/// not ours — the Lambda action carries no content.
/// </summary>
public class InboundGiftIdeasHandler
{
    private IServiceProvider? _serviceProvider;
    private readonly Lock _serviceProviderLock = new();

    public InboundGiftIdeasHandler() { }

    protected InboundGiftIdeasHandler(IServiceProvider serviceProvider)
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
    // Error capture only, and the reason is stronger here than anywhere else: this function
    // handles mail somebody sent in, so anything captured from it is a stranger's message.
    [Tracing(CaptureMode = TracingCaptureMode.Error)]
    public async Task FunctionHandler(SimpleEmailEvent<LambdaReceiptAction> sesEvent, ILambdaContext context)
    {
        var service = GetServiceProvider().GetService<InboundGiftIdeasService>()!;

        foreach (var record in sesEvent.Records)
        {
            var outcome = await service.ProcessRecordAsync(record).ConfigureAwait(false);

            context.Logger.LogInformation($"Inbound gift ideas message resolved as {outcome}.");
        }
    }
}
