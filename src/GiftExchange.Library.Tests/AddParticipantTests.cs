using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using dotenv.net;
using FluentAssertions;
using GiftExchange.Library.Handlers;
using GiftExchange.Library.Messaging;
using GiftExchange.Library.Tests.Fakes;
using GiftExchange.Library.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GiftExchange.Library.Tests;

public class AddParticipantTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDbFixture _dbFixture;

    private readonly IServiceProvider _serviceProvider;

    private readonly JsonService _jsonService;

    private readonly ILambdaContext _context;

    public AddParticipantTests(DynamoDbFixture dbFixture)
    {
        DotEnv.Load();

        _dbFixture = dbFixture;
        _context = new FakeLamdaContext();
        _jsonService = new JsonService();
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
        var innerRequest = new AddParticipantRequest
        {
            OrganizerEmail = "test@test.com",
            HatId = Guid.NewGuid(),
            Name = "Joe Test",
            Email = "participant@test.com"
        };

        var request = new APIGatewayProxyRequest
        {
            Body = _jsonService.SerializeDefault(innerRequest)
        };

        var sut = new AddParticipant(_serviceProvider);

        // act
        var response = await sut.FunctionHandler(request, _context);

        // assert
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }
}
