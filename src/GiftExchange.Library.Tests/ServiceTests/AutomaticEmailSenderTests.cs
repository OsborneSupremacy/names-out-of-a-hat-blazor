using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using GiftExchange.Library.Utility;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GiftExchange.Library.Tests.ServiceTests;

/// <summary>
/// The one promise this class makes: it sends, or it logs, and either way the caller carries on.
///
/// Everything calling it has already stored something durable by the time it runs, so an exception
/// escaping does not just lose a message — it fails the whole Lambda invocation and has the event
/// redelivered, which for inbound mail means the same submission stored twice.
/// </summary>
public class AutomaticEmailSenderTests
{
    static AutomaticEmailSenderTests()
    {
        DotEnv.Load();
        Environment.SetEnvironmentVariable("LIVE_MODE", "true");
    }

    private readonly IAmazonSimpleEmailService _ses = Substitute.For<IAmazonSimpleEmailService>();

    private readonly AutomaticEmailSender _sut;

    public AutomaticEmailSenderTests() =>
        _sut = new AutomaticEmailSender(_ses, Substitute.For<ILogger<AutomaticEmailSender>>());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<>")]
    [InlineData("not an address")]
    public async Task SendAsync_GivenARecipientThatIsNotAnAddress_SendsNothingAndDoesNotThrow(string recipient)
    {
        // act: an empty envelope sender is what a bounce carries, and it used to reach MimeKit and
        // throw out of the handler before the try block below was ever entered.
        await _sut.SendAsync(recipient, "Subject", "<p>Body</p>");

        // assert
        await _ses.DidNotReceive().SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_GivenSesRefuses_DoesNotThrow()
    {
        // arrange
        _ses.SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonSimpleEmailServiceException("no"));

        // act
        var send = async () => await _sut.SendAsync("person@example.com", "Subject", "<p>Body</p>");

        // assert
        await send.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_GivenAnAddress_SendsToIt()
    {
        // act
        await _sut.SendAsync("person@example.com", "Subject", "<p>Body</p>");

        // assert
        await _ses.Received(1).SendRawEmailAsync(Arg.Any<SendRawEmailRequest>(), Arg.Any<CancellationToken>());
    }
}
