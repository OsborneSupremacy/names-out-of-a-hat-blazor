using GiftExchange.Library.Handlers;
using GiftExchange.Library.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace GiftExchange.Library.Tests;

public class AddParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDbFixture _dbFixture;

    private readonly IServiceProvider _serviceProvider;

    public AddParticipantTests(DynamoDbFixture dbFixture)
    {
        _dbFixture = dbFixture;

        _serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddVendorServices()
            .BuildServiceProvider();
    }

    [Fact]
    public async Task AddParticipant_ValidRequest_ParticipantAdded()
    {
        // arrange
        var sut = new AddParticipant(_serviceProvider);


    }
}
