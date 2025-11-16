using Testcontainers.DynamoDb;

namespace GiftExchange.Library.Tests.Fixtures;

public class DynamoDbFixture : IAsyncLifetime
{
    public CancellationTokenSource CancellationTokenSource { get; }

    private readonly DynamoDbContainer _container;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    public DynamoDbFixture()
    {
        _container = new DynamoDbBuilder().Build();
        CancellationTokenSource = new();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync(CancellationTokenSource.Token);
    }

    public async Task DisposeAsync()
    {
        await CancellationTokenSource.CancelAsync();
        await _container.DisposeAsync();
        CancellationTokenSource.Dispose();
    }
}
