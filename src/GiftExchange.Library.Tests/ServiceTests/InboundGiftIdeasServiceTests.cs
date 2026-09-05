using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using GiftExchange.Library.Contexts;
using GiftExchange.Library.Utility;
using Microsoft.Extensions.Logging;
using MimeKit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

using InboundRecord = Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<
    Amazon.Lambda.SimpleEmailEvents.Actions.LambdaReceiptAction>.SimpleEmailRecord<
    Amazon.Lambda.SimpleEmailEvents.Actions.LambdaReceiptAction>;

namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// The inbound path end to end, against a real database.
///
/// What is worth pinning down here is not that each rule works — the parser and the policy have
/// their own tests — but which failures reach the sender and which end in silence, and in what
/// order the checks run. Both are security properties, and both are the kind of thing a later
/// refactor rearranges without noticing.
///
/// The provider is the real one rather than a substitute, because what is being exercised is a
/// token arriving in an address and resolving to a participant, their pick, and whoever drew them.
/// A stubbed lookup would only assert that the test knows its own arrangement.
/// </summary>
[Collection(PostgresCollection.Name)]
public class InboundGiftIdeasServiceTests
{
    private const string DoNotReply = "donotreply@mail.namesoutofahat.com";

    static InboundGiftIdeasServiceTests()
    {
        // Static, because the service reads its configuration in its own constructor and the
        // substitutes below are field initialisers, which run before any constructor body.
        DotEnv.Load();
        Environment.SetEnvironmentVariable("INBOUND_MAIL_BUCKET", "inbound-bucket");
        Environment.SetEnvironmentVariable("LIVE_MODE", "true");
    }

    private readonly IAmazonS3 _s3 = Substitute.For<IAmazonS3>();

    private readonly IAmazonSimpleEmailService _ses = Substitute.For<IAmazonSimpleEmailService>();

    private readonly IContentModerationService _moderation = Substitute.For<IContentModerationService>();

    private readonly IReplyThrottleProvider _throttle = Substitute.For<IReplyThrottleProvider>();

    /// <summary>
    /// Raw MIME captured as each send happens.
    /// </summary>
    /// <remarks>
    /// Copied at call time rather than read back off the recorded argument, because the service
    /// disposes the buffer it wrote — which is the right thing for it to do, and leaves nothing to
    /// read afterwards.
    /// </remarks>
    private readonly List<byte[]> _sent = [];

    private readonly GiftExchangeProvider _provider;

    private readonly IDbContextFactory<GiftExchangeDbContext> _contextFactory;

    private readonly InboundGiftIdeasService _sut;

    private readonly HatDataModelFaker _hatFaker = new();

    private readonly AddParticipantRequestFaker _participantFaker = new();

    public InboundGiftIdeasServiceTests(PostgresFixture dbFixture)
    {
        _contextFactory = dbFixture.CreateContextFactory();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(_contextFactory)
            .BuildServiceProvider();

        _provider = serviceProvider.GetRequiredService<GiftExchangeProvider>();

        _moderation.ValidateContentAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((true, string.Empty));
        _throttle.TryReserveReplySlotAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        _ses.When(ses => ses.SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>()))
            .Do(call =>
            {
                var buffer = new MemoryStream();
                var data = ((SendRawEmailRequest)call[0]).RawMessage.Data;
                data.Position = 0;
                data.CopyTo(buffer);
                _sent.Add(buffer.ToArray());
            });

        _sut = new InboundGiftIdeasService(
            _provider,
            _s3,
            new AutomaticEmailSender(_ses, Substitute.For<ILogger<AutomaticEmailSender>>()),
            _moderation,
            new InboundEmailParser(),
            new GiftIdeaContentPolicy(),
            new GiftIdeaEmailCompositionService(),
            _throttle,
            Substitute.For<ILogger<InboundGiftIdeasService>>());
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAGoodSubmission_StoresForwardsAndConfirms()
    {
        // arrange
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "A cast iron skillet, please.");

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.Shared);

        await using var context = _contextFactory.CreateDbContext();

        var stored = await context.GiftIdeas
            .Where(giftIdea => giftIdea.ParticipantId == exchange.AlphaId)
            .ToListAsync();

        stored.Should().ContainSingle().Which.Ideas.Should().Be("A cast iron skillet, please.");
        stored.Single().InboundMessageId.Should().Be("message-id", "so a report can find the original");

        // One to the person who drew them, one back to the sender.
        SentTo().Should().BeEquivalentTo([exchange.GammaEmail, exchange.AlphaEmail]);
    }

    [Fact]
    public async Task ProcessRecordAsync_StoresBeforeItSends()
    {
        // arrange: if sending fails after the write, the submission still exists and can go out
        // again. The other order loses what somebody wrote.
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "A scarf.");
        _ses.SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonSimpleEmailServiceException("SES is unavailable"));

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert: a send failure must not throw — throwing would have SES retry the whole message
        // and store it a second time.
        outcome.Should().Be(GiftIdeaSubmissionOutcome.Shared);

        await using var context = _contextFactory.CreateDbContext();
        (await context.GiftIdeas.AnyAsync(row => row.ParticipantId == exchange.AlphaId)).Should().BeTrue();
    }

    [Theory]
    [InlineData("FAIL", "PASS", "PASS", "PASS", "PASS")]
    [InlineData("PASS", "FAIL", "PASS", "PASS", "PASS")]
    [InlineData("PASS", "PASS", "FAIL", "PASS", "PASS")]
    [InlineData("PASS", "PASS", "PASS", "FAIL", "PASS")]
    [InlineData("PASS", "PASS", "PASS", "PASS", "FAIL")]
    [InlineData("PASS", "PASS", "PASS", "PASS", "GRAY")]
    public async Task ProcessRecordAsync_GivenAnyFailedVerdict_DropsItInSilence(
        string spam, string virusVerdict, string spf, string dkim, string dmarc)
    {
        // arrange
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "A scarf.");

        // act
        var outcome = await _sut.ProcessRecordAsync(
            RecordFor(exchange, spam, virusVerdict, spf, dkim, dmarc));

        // assert: no reply, because at this point From is whatever a spoofer chose to put there.
        // Answering it is how a mail system becomes a way of sending mail to strangers.
        outcome.Should().Be(GiftIdeaSubmissionOutcome.DroppedFailedAuthentication);
        await ShouldHaveStoredNothing(exchange);
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAnUnknownToken_DropsItInSilence()
    {
        // arrange
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "A scarf.");

        // act
        var outcome = await _sut.ProcessRecordAsync(
            RecordFor(exchange, recipient: $"{SecretToken.Create()}@ideas.namesoutofahat.com"));

        // assert: a reply would confirm which addresses exist, to whoever the message claims to be
        // from.
        outcome.Should().Be(GiftIdeaSubmissionOutcome.DroppedUnknownToken);
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAFromThatIsNotTheParticipants_DropsItInSilence()
    {
        // arrange: the token travels in an address that gets forwarded and quoted, so holding it is
        // a weaker claim than it looks. Pairing it with the participant's own address is what makes
        // it hold.
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "A scarf.", from: "someone.else@example.com");

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.DroppedSenderMismatch);
        await ShouldHaveStoredNothing(exchange);
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAnOutOfOffice_DropsItInSilence()
    {
        // arrange
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "I am out of the office until Monday.", autoSubmitted: true);

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert: replying would start a loop between two robots, and the text is not gift ideas.
        outcome.Should().Be(GiftIdeaSubmissionOutcome.DroppedAutomatedMessage);
        await ShouldHaveStoredNothing(exchange);
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenTextNamingTheirPick_RefusesItWithoutForwarding()
    {
        // arrange: somebody forwarded their invitation instead of using the button. Beta is who
        // Alpha drew, so this is Alpha's own secret coming back at us.
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "Ideas below.\nThe person picked for you is: Beta");

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedWouldRevealTheirPick);

        // The sender is told; the person who drew them hears nothing at all.
        SentTo().Should().BeEquivalentTo([exchange.AlphaEmail]);
        await ShouldHaveStoredNothing(exchange);
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenARefusal_TellsThemWhereToSendTheNextOneRatherThanToReply()
    {
        // arrange: a refusal used to end with "reply to this email", which was a dead end. The
        // refusal comes from the no-reply address, so a reply reaches the mailbox that answers only
        // that nobody read it — and that answer is throttled to one a day, so a second attempt gets
        // nothing at all.
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "Ideas below.\nThe person picked for you is: Beta");

        // act
        await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        var refusal = SentMessages().Single();

        refusal.From.Mailboxes.Single().Address.Should().Be(DoNotReply);

        refusal.HtmlBody.Should()
            .Contain($"{exchange.Token}@ideas.namesoutofahat.com", "the sender needs the address to write to")
            .And.Contain("SHARE GIFT IDEAS")
            .And.NotContain("Reply to this email");
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenARefusalOnAContribution_PointsAtTheAskAddress()
    {
        // arrange: Gamma was asked about Beta, and quotes their own invitation back at us. The way
        // back has to be the ask address they wrote to, not the one for their own wishes.
        var exchange = await SeedAsync();
        var ask = await GammaAskedAboutBetaAsync(exchange);

        GivenTheMessageIs(
            exchange,
            "The person picked for you is: Alpha",
            exchange.GammaEmail,
            token: ask.Token);

        // act
        var outcome = await _sut.ProcessRecordAsync(
            RecordFor(exchange, token: ask.Token, source: exchange.GammaEmail));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedWouldRevealTheirPick);

        var refusal = SentMessages().Single();

        refusal.To.Mailboxes.Single().Address.Should().Be(exchange.GammaEmail);
        refusal.HtmlBody.Should().Contain($"{ask.Token}@ideas.namesoutofahat.com").And.Contain("BETA");
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAClosedExchange_OffersNoWayToTryAgain()
    {
        // arrange: the one refusal nothing can be done about. A button inviting another message
        // would promise something that cannot happen, since the next one meets the same refusal.
        var exchange = await SeedAsync();
        await _provider.UpdateHatStatusAsync(exchange.OrganizerEmail, exchange.HatId, HatStatus.Closed);
        GivenTheMessageIs(exchange, "A scarf.");

        // act
        await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        SentMessages().Single().HtmlBody.Should().NotContain("SHARE GIFT IDEAS");
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenModerationRefusesIt_DoesNotStoreOrForward()
    {
        // arrange
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "Something unpleasant.");
        _moderation.ValidateContentAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((false, "no"));

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedInappropriateContent);
        SentTo().Should().BeEquivalentTo([exchange.AlphaEmail]);
        await ShouldHaveStoredNothing(exchange);
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAClosedExchange_TellsTheSenderRatherThanStoring()
    {
        // arrange
        var exchange = await SeedAsync();
        await _provider.UpdateHatStatusAsync(exchange.OrganizerEmail, exchange.HatId, HatStatus.Closed);
        GivenTheMessageIs(exchange, "A scarf.");

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedExchangeNotAcceptingIdeas);
        SentTo().Should().BeEquivalentTo([exchange.AlphaEmail]);
        await ShouldHaveStoredNothing(exchange);
    }

    [Fact]
    public async Task ProcessRecordAsync_ChecksTheSenderBeforeTheContent()
    {
        // arrange: a message from the wrong address whose text would also be refused on content.
        // The sender check has to win, or somebody holding a leaked token learns which of their
        // guesses about the exchange are right from which complaint comes back.
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "Picked for you is: Beta", from: "someone.else@example.com");

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.DroppedSenderMismatch);
    }

    [Fact]
    public async Task ProcessRecordAsync_MarksEveryMessageItSendsAsAutomatic()
    {
        // arrange
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "A scarf.");

        // act
        await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert: RFC 3834. Without it, an out of office at the far end answers ours and ours
        // answers back.
        var headers = SentMessages().Select(message => message.Headers["Auto-Submitted"]).ToList();

        headers.Should().NotBeEmpty();
        headers.Should().AllBe("auto-replied");
    }

    [Fact]
    public async Task ProcessRecordAsync_GivesTheForwardNoWayBackToTheSender()
    {
        // arrange
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "A scarf.");

        // act
        await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert: a reply that reached the sender would tell them who holds their name, which is
        // the one thing this application exists to keep quiet.
        var forward = SentMessages()
            .Single(message => message.To.Mailboxes.Any(mailbox => mailbox.Address == exchange.GammaEmail));

        forward.ReplyTo.Count.Should().Be(0, "a reply must have nowhere to go");

        var fromAddress = forward.From.Mailboxes.Single().Address;
        fromAddress.Should().Be(DoNotReply);

        // Named, because the recipient already knows whose name they drew.
        forward.HtmlBody.Should().Contain("Alpha");
    }

    [Fact]
    public async Task ProcessRecordAsync_EchoesBackExactlyWhatWasStored()
    {
        // arrange: the stored text is a guess at where the sender's message ended and a quoted one
        // began. Showing them what was kept turns a bad guess into something they can correct.
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "A cast iron skillet, please.");

        // act
        await _sut.ProcessRecordAsync(RecordFor(exchange));

        // assert
        var confirmation = SentMessages()
            .Single(message => message.To.Mailboxes.Any(mailbox => mailbox.Address == exchange.AlphaEmail));

        confirmation.HtmlBody.Should().Contain("A cast iron skillet, please.");
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenMailToTheDoNotReplyAddress_PointsThemAtTheButton()
    {
        // arrange
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "Can you tell Ben I can't make it?");

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange, recipient: DoNotReply));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RedirectedFromDoNotReply);
        SentMessages().Single().HtmlBody.Should().Contain("SHARE GIFT IDEAS");
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenTheDoNotReplyThrottleIsSpent_StaysQuiet()
    {
        // arrange: the one reply sent to an address that is not a known participant's, so it is the
        // one that could be pointed at a stranger repeatedly.
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "Hello?");
        _throttle.TryReserveReplySlotAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange, recipient: DoNotReply));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RedirectedFromDoNotReply);
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("<>")]
    public async Task ProcessRecordAsync_GivenMailToTheDoNotReplyAddressWithNoReturnPath_StaysQuiet(string source)
    {
        // arrange: a bounce, which carries an empty envelope sender so that nothing answers it.
        var exchange = await SeedAsync();
        GivenTheMessageIs(exchange, "Delivery to the following recipient failed permanently.");

        // act
        var outcome = await _sut.ProcessRecordAsync(RecordFor(exchange, recipient: DoNotReply, source: source));

        // assert
        outcome.Should().Be(GiftIdeaSubmissionOutcome.DroppedAutomatedMessage);
        await _throttle.DidNotReceive().TryReserveReplySlotAsync(Arg.Any<string>(), Arg.Any<string>());
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAContribution_SendsItToTheAskerAndNotToTheirOwnGiver()
    {
        // arrange: Alpha drew Beta and asked Gamma what Beta might like.
        var exchange = await SeedAsync();
        var ask = await GammaAskedAboutBetaAsync(exchange);

        GivenTheMessageIs(exchange, "Beta has been after a stand mixer.", exchange.GammaEmail, token: ask.Token);

        // act
        var outcome = await _sut.ProcessRecordAsync(
            RecordFor(exchange, token: ask.Token, source: exchange.GammaEmail));

        // assert: the whole point of the routing. Beta drew Gamma, so anything Gamma writes about
        // themselves goes to Beta — but this is not about Gamma, and sending it to Beta would hand
        // Beta a list of the gifts somebody is planning to buy them.
        outcome.Should().Be(GiftIdeaSubmissionOutcome.Shared);

        var forward = SentMessages()
            .Single(message => message.To.Mailboxes.Single().Address == exchange.AlphaEmail);

        forward.Subject.Should().Contain("Gamma").And.Contain("Beta");
        forward.HtmlBody.Should().Contain("stand mixer");

        SentTo().Should().BeEquivalentTo([exchange.AlphaEmail, exchange.GammaEmail]);
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAContribution_StoresItApartFromTheirOwnWishes()
    {
        // arrange
        var exchange = await SeedAsync();
        var ask = await GammaAskedAboutBetaAsync(exchange);

        GivenTheMessageIs(exchange, "Beta has been after a stand mixer.", exchange.GammaEmail, token: ask.Token);

        // act
        await _sut.ProcessRecordAsync(RecordFor(exchange, token: ask.Token, source: exchange.GammaEmail));

        // assert: filed against the ask, not against Gamma. A row in gift_idea would be Gamma
        // saying what Gamma wants, and would be forwarded as such the moment anybody read it back.
        await using var context = _contextFactory.CreateDbContext();

        var stored = await context.ContributedGiftIdeas
            .Where(contribution => contribution.GiftIdeaAskId == ask.AskId)
            .ToListAsync();

        stored.Should().ContainSingle().Which.Ideas.Should().Be("Beta has been after a stand mixer.");

        (await context.GiftIdeas.AnyAsync(row => row.ParticipantId == exchange.GammaId))
            .Should().BeFalse("a suggestion about somebody else is not a wish of their own");
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAContributionFromSomebodyElse_DropsItInSilence()
    {
        // arrange: the ask was addressed to Gamma, and Beta is writing on it.
        var exchange = await SeedAsync();
        var ask = await GammaAskedAboutBetaAsync(exchange);

        GivenTheMessageIs(exchange, "Ideas!", exchange.AlphaEmail, token: ask.Token);

        // act
        var outcome = await _sut.ProcessRecordAsync(
            RecordFor(exchange, token: ask.Token, source: exchange.AlphaEmail));

        // assert: an ask address travels in an inbox and can be forwarded, so the token alone is a
        // weaker claim than it looks. Pairing it with the helper's own address is what makes it
        // hold — and the reply is silence, because nothing here has established who wrote in.
        outcome.Should().Be(GiftIdeaSubmissionOutcome.DroppedSenderMismatch);

        SentTo().Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessRecordAsync_GivenAContributionNamingTheHelpersOwnPick_RefusesIt()
    {
        // arrange: Gamma drew Alpha, and Alpha is who this contribution is about to be sent to.
        var exchange = await SeedAsync();
        var ask = await GammaAskedAboutBetaAsync(exchange);

        GivenTheMessageIs(
            exchange, "Beta likes what Alpha likes.", exchange.GammaEmail, token: ask.Token);

        // act
        var outcome = await _sut.ProcessRecordAsync(
            RecordFor(exchange, token: ask.Token, source: exchange.GammaEmail));

        // assert: the check still guards the sender's own pick rather than the subject, and on this
        // path that is doing more work than usual — the person it would leak to is the very person
        // the message is being forwarded to.
        outcome.Should().Be(GiftIdeaSubmissionOutcome.RejectedWouldRevealTheirPick);

        SentTo().Should().BeEquivalentTo([exchange.GammaEmail]);
    }

    /// <summary>Alpha, who drew Beta, asks Gamma what Beta might like.</summary>
    private async Task<(Guid AskId, string Token)> GammaAskedAboutBetaAsync(SeededExchange exchange)
    {
        var token = await _provider.IssueGiftIdeaAskAsync(exchange.AlphaId, exchange.GammaId, exchange.BetaId);

        await using var context = _contextFactory.CreateDbContext();

        var askId = await context.GiftIdeaAsks
            .Where(ask => ask.TokenHash == SecretToken.Hash(token))
            .Select(ask => ask.GiftIdeaAskId)
            .SingleAsync();

        return (askId, token);
    }

    private async Task ShouldHaveStoredNothing(SeededExchange exchange)
    {
        await using var context = _contextFactory.CreateDbContext();
        (await context.GiftIdeas.AnyAsync(row => row.ParticipantId == exchange.AlphaId)).Should().BeFalse();
    }

    /// <summary>
    /// A hat drawing in a three-way cycle — Alpha drew Beta, Beta drew Gamma, Gamma drew Alpha —
    /// with tokens issued. Three rather than two so that who Alpha drew and who drew Alpha are
    /// different people, which is what makes the routing assertions mean anything.
    /// </summary>
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

        var ids = await context.Participants
            .Where(participant => participant.HatId == hat.HatId)
            .Select(participant => new { participant.ParticipantId, participant.Person.Email })
            .ToDictionaryAsync(row => row.Email, row => row.ParticipantId);

        return new SeededExchange(
            hat.HatId,
            hat.OrganizerEmail,
            ids[alpha],
            alpha,
            ids[beta],
            ids[gamma],
            gamma,
            tokens[alpha]);
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

    private void GivenTheMessageIs(
        SeededExchange exchange,
        string body,
        string? from = null,
        bool autoSubmitted = false,
        string? token = null
    )
    {
        var message = new MimeMessage
        {
            Subject = "My gift ideas",
            Body = new TextPart("plain") { Text = body }
        };

        message.From.Add(MailboxAddress.Parse(from ?? exchange.AlphaEmail));
        message.To.Add(MailboxAddress.Parse($"{token ?? exchange.Token}@ideas.namesoutofahat.com"));

        if (autoSubmitted)
            message.Headers.Add("Auto-Submitted", "auto-replied");

        var buffer = new MemoryStream();
        message.WriteTo(buffer);

        _s3.GetObjectAsync("inbound-bucket", "message-id", Arg.Any<CancellationToken>())
            .Returns(_ => new GetObjectResponse { ResponseStream = new MemoryStream(buffer.ToArray()) });
    }

    private static InboundRecord RecordFor(
        SeededExchange exchange,
        string spam = "PASS",
        string virusVerdict = "PASS",
        string spf = "PASS",
        string dkim = "PASS",
        string dmarc = "PASS",
        string? recipient = null,
        string? token = null,
        string? source = null
    )
    {
        var to = recipient ?? $"{token ?? exchange.Token}@ideas.namesoutofahat.com";

        return new InboundRecord
        {
            Ses = new SimpleEmailEvent<LambdaReceiptAction>.SimpleEmailService<LambdaReceiptAction>
            {
                Mail = new SimpleEmailEvent<LambdaReceiptAction>.SimpleEmailMessage
                {
                    MessageId = "message-id",
                    Source = source ?? exchange.AlphaEmail,
                    Destination = [to]
                },
                Receipt = new SimpleEmailEvent<LambdaReceiptAction>.SimpleEmailReceipt<LambdaReceiptAction>
                {
                    Recipients = [to],
                    SpamVerdict = new SimpleEmailVerdict { Status = spam },
                    VirusVerdict = new SimpleEmailVerdict { Status = virusVerdict },
                    SPFVerdict = new SimpleEmailVerdict { Status = spf },
                    DKIMVerdict = new SimpleEmailVerdict { Status = dkim },
                    DMARCVerdict = new SimpleEmailVerdict { Status = dmarc }
                }
            }
        };
    }

    /// <summary>The messages actually handed to SES, parsed back out of the captured MIME.</summary>
    private ImmutableList<MimeMessage> SentMessages() =>
        [.. _sent.Select(raw => MimeMessage.Load(new MemoryStream(raw)))];

    private ImmutableList<string> SentTo() =>
        [.. SentMessages().SelectMany(message => message.To.Mailboxes).Select(mailbox => mailbox.Address)];

    private sealed record SeededExchange(
        Guid HatId,
        string OrganizerEmail,
        Guid AlphaId,
        string AlphaEmail,
        Guid BetaId,
        Guid GammaId,
        string GammaEmail,
        string Token
    );
}
