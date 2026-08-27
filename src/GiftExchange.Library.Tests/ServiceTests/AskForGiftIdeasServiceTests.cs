using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using GiftExchange.Library.Contexts;
using GiftExchange.Library.Utility;
using Microsoft.Extensions.Logging;
using MimeKit;
using NSubstitute;

namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// The Ask, against a real database.
///
/// Two properties carry most of the weight here, and neither is visible from the happy path alone:
/// that a GET never sends anything, and that nothing reaching the person being asked names the
/// person who asked.
/// </summary>
[Collection(PostgresCollection.Name)]
public class AskForGiftIdeasServiceTests
{
    static AskForGiftIdeasServiceTests()
    {
        DotEnv.Load();
        Environment.SetEnvironmentVariable("LIVE_MODE", "true");
    }

    private readonly IAmazonSimpleEmailService _ses = Substitute.For<IAmazonSimpleEmailService>();

    private readonly IReplyThrottleProvider _throttle = Substitute.For<IReplyThrottleProvider>();

    private readonly List<byte[]> _sent = [];

    private readonly GiftExchangeProvider _provider;

    private readonly IDbContextFactory<GiftExchangeDbContext> _contextFactory;

    private readonly AskForGiftIdeasService _sut;

    private readonly HatDataModelFaker _hatFaker = new();

    private readonly AddParticipantRequestFaker _participantFaker = new();

    public AskForGiftIdeasServiceTests(PostgresFixture dbFixture)
    {
        _contextFactory = dbFixture.CreateContextFactory();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(_contextFactory)
            .BuildServiceProvider();

        _provider = serviceProvider.GetRequiredService<GiftExchangeProvider>();

        _throttle.TryReserveAskSlotAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>())
            .Returns((true, DateTimeOffset.MinValue));

        _ses.When(ses => ses.SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var buffer = new MemoryStream();
                var data = ((SendRawEmailRequest)call[0]).RawMessage.Data;
                data.Position = 0;
                data.CopyTo(buffer);
                _sent.Add(buffer.ToArray());
            });

        _sut = new AskForGiftIdeasService(
            _provider,
            _throttle,
            new GiftIdeaEmailCompositionService(),
            new AskPageComposer(),
            new AutomaticEmailSender(_ses, Substitute.For<ILogger<AutomaticEmailSender>>()),
            Substitute.For<ILogger<AskForGiftIdeasService>>());
    }

    [Fact]
    public async Task Get_ShowsAConfirmationAndSendsNothing()
    {
        // arrange
        var exchange = await SeedAsync();

        // act
        var response = await _sut.FunctionHandler(Request("GET", exchange.AlphaToken), new FakeLambdaContext());

        // assert: the whole reason this is two endpoints. A mail scanner following the link in an
        // invitation gets this page, and nobody is emailed on the participant's behalf.
        response.StatusCode.Should().Be(200);
        response.Body.Should().Contain("Yes, ask Beta");
        response.Body.Should().Contain("<form method=\"post\"");

        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
        await _throttle.DidNotReceive().TryReserveAskSlotAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task Post_AsksThePersonTheParticipantDrew()
    {
        // arrange
        var exchange = await SeedAsync();

        // act
        var response = await _sut.FunctionHandler(Request("POST", exchange.AlphaToken), new FakeLambdaContext());

        // assert
        response.Body.Should().Contain("We've asked Beta");

        var ask = SentMessages().Single();

        ask.To.Mailboxes.Single().Address.Should().Be(exchange.BetaEmail);
    }

    [Fact]
    public async Task Post_NeverNamesTheAsker()
    {
        // arrange
        var exchange = await SeedAsync();

        // act
        await _sut.FunctionHandler(Request("POST", exchange.AlphaToken), new FakeLambdaContext());

        // assert: the promise the button makes. In an exchange this small, naming the asker names
        // the one person holding this recipient's name.
        var ask = SentMessages().Single();

        ask.HtmlBody.Should().NotContain("Alpha");
        ask.HtmlBody.Should().NotContain(exchange.AlphaEmail);
        ask.Subject.Should().NotContain("Alpha");
        ask.HtmlBody.Should().Contain("Someone in");
    }

    [Fact]
    public async Task Post_GivesThemAWorkingAddressOfTheirOwn()
    {
        // arrange
        var exchange = await SeedAsync();

        // act
        await _sut.FunctionHandler(Request("POST", exchange.AlphaToken), new FakeLambdaContext());

        // assert: the point of the email. It carries the same SHARE GIFT IDEAS block the invitation
        // does, addressed to a token issued for them.
        var ask = SentMessages().Single();

        ask.HtmlBody.Should().Contain("SHARE GIFT IDEAS");
        ask.HtmlBody.Should().Contain("@ideas.namesoutofahat.com");

        await using var context = _contextFactory.CreateDbContext();

        var tokens = await context.GiftIdeaTokens
            .CountAsync(token => token.ParticipantId == exchange.BetaId);

        // Alongside the one they were issued with their invitation, not instead of it — theirs
        // cannot be reconstructed, and replacing it would kill the address already in their inbox.
        tokens.Should().Be(2);
    }

    [Fact]
    public async Task Post_LeavesTheOriginalAddressWorking()
    {
        // arrange
        var exchange = await SeedAsync();

        // act
        await _sut.FunctionHandler(Request("POST", exchange.AlphaToken), new FakeLambdaContext());

        // assert: the address in Beta's invitation still resolves to Beta.
        var (found, route) = await _provider.FindGiftIdeaRouteAsync(SecretToken.Hash(exchange.BetaToken));

        found.Should().BeTrue();
        route.ParticipantId.Should().Be(exchange.BetaId);
    }

    [Fact]
    public async Task Post_GivenTheThrottleRefuses_TellsTheAskerWhenTheyLastAsked()
    {
        // arrange
        var askedOn = new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero);
        _throttle.TryReserveAskSlotAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>()).Returns((false, askedOn));

        var exchange = await SeedAsync();

        // act
        var response = await _sut.FunctionHandler(Request("POST", exchange.AlphaToken), new FakeLambdaContext());

        // assert: the person being asked hears nothing at all; the asker gets the date, because the
        // likeliest reader is somebody who does not remember asking.
        var message = SentMessages().Single();

        message.To.Mailboxes.Single().Address.Should().Be(exchange.AlphaEmail);
        message.HtmlBody.Should().Contain("3 August 2026");
        response.Body.Should().Contain("3 August 2026");
    }

    [Fact]
    public async Task Post_GivenTheThrottleRefuses_IssuesNoTokenAndAsksNobody()
    {
        // arrange
        _throttle.TryReserveAskSlotAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>())
            .Returns((false, DateTimeOffset.UtcNow.AddDays(-2)));

        var exchange = await SeedAsync();

        // act
        await _sut.FunctionHandler(Request("POST", exchange.AlphaToken), new FakeLambdaContext());

        // assert
        SentMessages().Should().AllSatisfy(message =>
            message.To.Mailboxes.Single().Address.Should().Be(exchange.AlphaEmail));

        await using var context = _contextFactory.CreateDbContext();

        (await context.GiftIdeaTokens.CountAsync(token => token.ParticipantId == exchange.BetaId))
            .Should().Be(1, "a refused Ask must not leave a token behind");
    }

    [Fact]
    public async Task Post_GivenAClosedExchange_SendsNothing()
    {
        // arrange
        var exchange = await SeedAsync();
        await _provider.UpdateHatStatusAsync(exchange.OrganizerEmail, exchange.HatId, HatStatus.Closed);

        // act
        var response = await _sut.FunctionHandler(Request("POST", exchange.AlphaToken), new FakeLambdaContext());

        // assert
        response.Body.Should().Contain("the link may have expired");
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task GivenAnUnknownToken_SaysTheSameThingAsAFinishedExchange(string method)
    {
        // arrange
        await SeedAsync();

        // act
        var response = await _sut.FunctionHandler(
            Request(method, SecretToken.Create()), new FakeLambdaContext());

        // assert: indistinguishable on purpose. Somebody holding a guessed token would otherwise
        // learn from the difference whether it named a real participant.
        response.StatusCode.Should().Be(200);
        response.Body.Should().Contain("the link may have expired");
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_IsNotCached()
    {
        // arrange
        var exchange = await SeedAsync();

        // act
        var response = await _sut.FunctionHandler(Request("GET", exchange.AlphaToken), new FakeLambdaContext());

        // assert: a cached page would report an outcome that is no longer true.
        response.Headers["Cache-Control"].Should().Be("no-store");
        response.Headers["Content-Type"].Should().Be("text/html; charset=utf-8");
    }

    private static APIGatewayProxyRequest Request(string method, string token) =>
        new()
        {
            HttpMethod = method,
            Resource = "/ask/{token}",
            PathParameters = new Dictionary<string, string> { ["token"] = token }
        };

    private ImmutableList<MimeMessage> SentMessages() =>
        [.. _sent.Select(raw => MimeMessage.Load(new MemoryStream(raw)))];

    /// <summary>Alpha drew Beta, Beta drew Gamma, Gamma drew Alpha, with tokens issued.</summary>
    private async Task<SeededExchange> SeedAsync()
    {
        var hat = _hatFaker.Generate();
        await _provider.CreateHatAsync(hat);

        var alpha = await AddAsync(hat, "Alpha");
        var beta = await AddAsync(hat, "Beta");
        var gamma = await AddAsync(hat, "Gamma");

        await _provider.UpdateParticipantPickedRecipientAsync(hat.OrganizerEmail, hat.HatId, alpha, "Beta");
        await _provider.UpdateParticipantPickedRecipientAsync(hat.OrganizerEmail, hat.HatId, beta, "Gamma");
        await _provider.UpdateParticipantPickedRecipientAsync(hat.OrganizerEmail, hat.HatId, gamma, "Alpha");
        await _provider.UpdateHatStatusAsync(hat.OrganizerEmail, hat.HatId, HatStatus.InvitationsSent);

        var tokens = await _provider.IssueGiftIdeaTokensAsync(hat.HatId);

        await using var context = _contextFactory.CreateDbContext();

        var betaId = await context.Participants
            .Where(participant => participant.HatId == hat.HatId && participant.Person.Email == beta)
            .Select(participant => participant.ParticipantId)
            .SingleAsync();

        return new SeededExchange(
            hat.HatId, hat.OrganizerEmail, alpha, tokens[alpha], betaId, beta, tokens[beta]);
    }

    private async Task<string> AddAsync(HatDataModel hat, string name)
    {
        var request = _participantFaker.Generate() with
        {
            HatId = hat.HatId, OrganizerEmail = hat.OrganizerEmail, Name = name
        };

        await _provider.CreateParticipantAsync(request, []);

        return request.Email;
    }

    private sealed record SeededExchange(
        Guid HatId,
        string OrganizerEmail,
        string AlphaEmail,
        string AlphaToken,
        Guid BetaId,
        string BetaEmail,
        string BetaToken
    );
}
