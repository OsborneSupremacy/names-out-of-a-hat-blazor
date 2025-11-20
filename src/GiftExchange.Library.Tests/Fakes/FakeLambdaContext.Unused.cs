using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace GiftExchange.Library.Tests.Fakes;

[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
internal partial class FakeLambdaContext
{
    public string AwsRequestId { get; }

    public IClientContext ClientContext { get; }

    public string FunctionName { get; }

    public string FunctionVersion { get; }

    public ICognitoIdentity Identity { get; }

    public string InvokedFunctionArn { get; }

    public ILambdaLogger Logger { get; }

    public string LogGroupName { get; }

    public string LogStreamName { get; }

    public int MemoryLimitInMB { get; }

    public TimeSpan RemainingTime { get; }
}
