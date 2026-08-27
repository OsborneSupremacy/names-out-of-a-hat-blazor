using MimeKit;
using System.Text;

namespace GiftExchange.Library.Tests.ServiceTests;

public class InboundEmailParserTests
{
    private readonly InboundEmailParser _sut = new();

    [Fact]
    public void Parse_ReadsTheSenderAndTheirText()
    {
        // act
        var email = Parse(
            from: "Alice <ALICE@example.com>",
            body: "A cast iron skillet, please.");

        // assert: lower-cased, because it is compared against a stored address.
        email.From.Should().Be("alice@example.com");
        email.Body.Should().Be("A cast iron skillet, please.");
        email.IsAutomated.Should().BeFalse();
    }

    [Theory]
    // Gmail and Apple Mail.
    [InlineData("On Mon, 1 Dec 2025 at 09:14, Names Out Of A Hat <donotreply@mail.namesoutofahat.com> wrote:")]
    // Outlook.
    [InlineData("-----Original Message-----")]
    [InlineData("________________________________")]
    [InlineData("From: Names Out Of A Hat <donotreply@mail.namesoutofahat.com>")]
    public void Parse_CutsEverythingFromTheStartOfAQuotedMessage(string marker)
    {
        // arrange: the line after the marker is the one that matters. An invitation names the
        // person the sender drew, and forwarding that would tell the recipient who it is.
        var body = $"""
                    A cast iron skillet, please.

                    {marker}
                    The person whose name was picked out of a hat for you is: Charlie
                    """;

        // act
        var email = Parse("alice@example.com", body);

        // assert
        email.Body.Should().Be("A cast iron skillet, please.");
        email.Body.Should().NotContain("Charlie");
    }

    [Fact]
    public void Parse_DropsPrefixQuotedLinesLeftBehindTheCut()
    {
        // arrange
        var body = """
                   Something warm, please.

                   > The person whose name was picked out of a hat for you is: Charlie
                   > Please purchase a gift in the range of $25 - $40.
                   """;

        // act
        var email = Parse("alice@example.com", body);

        // assert
        email.Body.Should().Be("Something warm, please.");
    }

    [Fact]
    public void Parse_TrimsASignatureBlock()
    {
        // arrange
        var body = "A bread book.\n\n--\nAlice\nSent from somewhere";

        // act
        var email = Parse("alice@example.com", body);

        // assert
        email.Body.Should().Be("A bread book.");
    }

    [Fact]
    public void Parse_LeavesOrdinarySentencesBeginningWithOnAlone()
    {
        // arrange: the quote pattern is deliberately narrow. Silently halving somebody's message at
        // the first sentence starting "On" is worse than leaving a quote in, because the policy
        // check refuses anything still naming their pick anyway.
        const string body = "On reflection, I would like a scarf. Only if it is on sale though.";

        // act
        var email = Parse("alice@example.com", body);

        // assert
        email.Body.Should().Be(body);
    }

    [Theory]
    [InlineData("Auto-Submitted", "auto-replied")]
    [InlineData("X-Autoreply", "yes")]
    [InlineData("Precedence", "bulk")]
    [InlineData("List-Id", "<announce.example.com>")]
    public void Parse_RecognisesMachineGeneratedMail(string header, string value)
    {
        // act: an out of office landing at a gift ideas address would otherwise be stored as
        // somebody's gift ideas and forwarded to the person who drew them.
        var email = Parse("alice@example.com", "I am out of the office until Monday.", (header, value));

        // assert
        email.IsAutomated.Should().BeTrue();
    }

    [Fact]
    public void Parse_TreatsAutoSubmittedNoAsAHumanSendingIt()
    {
        // act: RFC 3834 says "no" is the value an ordinary message carries, so it must not be read
        // as present-means-automated the way the vendor headers are.
        var email = Parse("alice@example.com", "A scarf, please.", ("Auto-Submitted", "no"));

        // assert
        email.IsAutomated.Should().BeFalse();
    }

    [Fact]
    public void ConvertHtmlToText_WritesOutWhereALinkActuallyGoes()
    {
        // arrange: the whole point. Anchor text can say anything, and a conversion that kept the
        // words and dropped the href would keep the lie and discard the truth.
        const string html = """<p>I would like <a href="https://evil.example/steal">amazon.com</a></p>""";

        // act
        var text = InboundEmailParser.ConvertHtmlToText(html);

        // assert
        text.Should().Contain("https://evil.example/steal");
        text.Should().Contain("amazon.com");
    }

    [Fact]
    public void ConvertHtmlToText_LeavesOutScriptAndStyleContent()
    {
        // arrange
        const string html = """
                            <style>.x { color: red }</style>
                            <p>A scarf</p>
                            <script>alert('hello')</script>
                            """;

        // act
        var text = InboundEmailParser.ConvertHtmlToText(html);

        // assert: neither was written by the sender to be read, and both would otherwise land in
        // the message as though they had been.
        text.Should().Contain("A scarf");
        text.Should().NotContain("color: red");
        text.Should().NotContain("alert");
    }

    [Fact]
    public void ConvertHtmlToText_DecodesEntitiesRatherThanPassingThemOn()
    {
        // act
        var text = InboundEmailParser.ConvertHtmlToText("<p>Tea &amp; biscuits &lt;the good ones&gt;</p>");

        // assert
        text.Should().Contain("Tea & biscuits <the good ones>");
    }

    [Fact]
    public void Parse_FallsBackToTheHtmlPartWhenThereIsNoPlainText()
    {
        // arrange
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse("alice@example.com"));
        message.To.Add(MailboxAddress.Parse("token@ideas.namesoutofahat.com"));
        message.Subject = "My gift ideas";
        message.Body = new TextPart("html") { Text = """<p>A <a href="https://example.com/list">wishlist</a></p>""" };

        // act
        var email = _sut.Parse(ToStream(message));

        // assert
        email.Body.Should().Contain("wishlist");
        email.Body.Should().Contain("https://example.com/list");
    }

    [Fact]
    public void Parse_NamesRealAttachmentsAndIgnoresInlineSignatureImages()
    {
        // arrange
        var builder = new BodyBuilder { TextBody = "Here's what I'd like." };

        builder.Attachments.Add("wishlist.pdf", "%PDF-1.4 not really"u8.ToArray());

        // A logo in an email signature. The sender did not attach it and would be baffled to be
        // told it was dropped, so it must not be counted.
        var logo = builder.LinkedResources.Add("logo.png", new byte[] { 1, 2, 3 });
        logo.ContentId = "logo";

        var message = new MimeMessage { Subject = "My gift ideas", Body = builder.ToMessageBody() };
        message.From.Add(MailboxAddress.Parse("alice@example.com"));
        message.To.Add(MailboxAddress.Parse("token@ideas.namesoutofahat.com"));

        // act
        var email = _sut.Parse(ToStream(message));

        // assert
        email.AttachmentNames.Should().ContainSingle().Which.Should().Be("wishlist.pdf");
    }

    private InboundEmail Parse(string from, string body, params (string Name, string Value)[] headers)
    {
        var message = new MimeMessage
        {
            Subject = "My gift ideas",
            Body = new TextPart("plain") { Text = body }
        };

        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse("token@ideas.namesoutofahat.com"));

        foreach (var (name, value) in headers)
            message.Headers.Add(name, value);

        return _sut.Parse(ToStream(message));
    }

    private static Stream ToStream(MimeMessage message)
    {
        var buffer = new MemoryStream();
        message.WriteTo(buffer);
        buffer.Position = 0;
        return buffer;
    }
}
