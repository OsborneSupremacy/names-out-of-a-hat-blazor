using System.Text;
using System.Text.RegularExpressions;
using MimeKit;
using MimeKit.Text;

namespace GiftExchange.Library.Services;

/// <summary>
/// Turns a raw received message into the few things a decision can be made from.
/// </summary>
/// <remarks>
/// Everything here is defensive rather than clever. A message arrives written by software nobody
/// here chose, in a shape nobody here agreed to, from somebody who may not be who they say. What
/// comes out the other side is plain text, with no markup, no remote references and no attachments.
/// </remarks>
[UsedImplicitly]
internal partial class InboundEmailParser
{
    /// <summary>
    /// Headers by which well-behaved software announces that a human did not send this. Honouring
    /// them is also what keeps our own replies from starting a loop, since ours say the same.
    /// </summary>
    private static readonly ImmutableList<string> AutomatedHeaders =
    [
        "Auto-Submitted",
        "X-Autoreply",
        "X-Autorespond",
        "X-Auto-Response-Suppress",
        "List-Id",
        "List-Unsubscribe"
    ];

    public InboundEmail Parse(Stream rawMessage)
    {
        var message = MimeMessage.Load(rawMessage);

        // Plain text if the sender's client offered it, which nearly all do as one half of a
        // multipart/alternative. Converting the HTML is the fallback, not the first choice.
        var body = !string.IsNullOrWhiteSpace(message.TextBody)
            ? message.TextBody
            : ConvertHtmlToText(message.HtmlBody ?? string.Empty);

        return new InboundEmail
        {
            From = message.From.Mailboxes.FirstOrDefault()?.Address.TrimNullSafe().ToLowerInvariant()
                   ?? string.Empty,
            Body = StripQuotedReplyAndSignature(body).Trim(),
            AttachmentNames = ReadAttachmentNames(message),
            IsAutomated = IsAutomated(message)
        };
    }

    /// <summary>
    /// Whether the message says a machine sent it.
    /// </summary>
    /// <remarks>
    /// <c>Auto-Submitted</c> is the one that is actually specified, in RFC 3834, and its value is
    /// only meaningful when it is something other than "no". The rest are conventions various
    /// vendors settled on instead, and are treated as present-means-automated.
    /// </remarks>
    private static bool IsAutomated(MimeMessage message)
    {
        var autoSubmitted = message.Headers["Auto-Submitted"];

        if (!string.IsNullOrWhiteSpace(autoSubmitted)
            && !autoSubmitted.Trim().StartsWith("no", StringComparison.OrdinalIgnoreCase))
            return true;

        if (AutomatedHeaders
            .Where(header => !header.Equals("Auto-Submitted", StringComparison.OrdinalIgnoreCase))
            .Any(header => !string.IsNullOrWhiteSpace(message.Headers[header])))
            return true;

        // Not a header anybody sets deliberately to mean this, but bulk senders set it and humans
        // do not, and a mailing list arriving here is no more a gift idea than an out of office is.
        var precedence = message.Headers["Precedence"];

        return !string.IsNullOrWhiteSpace(precedence)
               && precedence.Trim() is var value
               && (value.Equals("bulk", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("auto_reply", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("junk", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Files the sender attached, ignoring the inline parts that make up the message itself.
    /// </summary>
    /// <remarks>
    /// MimeKit counts a part as an attachment only when its Content-Disposition says so, which is
    /// what keeps the logo in somebody's email signature out of this list. Those arrive as inline
    /// parts referenced by a cid: in the HTML, and reporting them would tell most corporate senders
    /// that an attachment they never made was dropped.
    /// </remarks>
    private static ImmutableList<string> ReadAttachmentNames(MimeMessage message) =>
    [
        .. message.Attachments
            .Select(attachment =>
                attachment.ContentDisposition?.FileName
                ?? attachment.ContentType?.Name
                ?? "unnamed attachment")
            .Where(name => !string.IsNullOrWhiteSpace(name))
    ];

    /// <summary>
    /// Converts an HTML body to text, expanding every link so its true destination is visible.
    /// </summary>
    /// <remarks>
    /// The expansion is the point, and it is a security property rather than a formatting one. An
    /// anchor can say "amazon.com" and go anywhere, and a naive conversion keeps the words and
    /// discards the address — which is to say it keeps the lie and throws away the truth. Writing
    /// the href out beside the text means whoever receives these ideas can see where a link goes
    /// before deciding whether to follow it, which is the single most useful thing this application
    /// can do about links it has no way to vet.
    ///
    /// Driven by MimeKit's tokenizer rather than by pattern matching over the markup. Hostile HTML
    /// is exactly the input that defeats a regular expression.
    /// </remarks>
    internal static string ConvertHtmlToText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = new StringBuilder();
        var tokenizer = new HtmlTokenizer(new StringReader(html));
        var suppressed = 0;

        while (tokenizer.ReadNextToken(out var token))
        {
            switch (token.Kind)
            {
                case HtmlTokenKind.Tag:
                    var tag = (HtmlTagToken)token;

                    // Script and style carry text that was never meant to be read. Their content
                    // would otherwise land in the message as though the sender had typed it.
                    if (tag.Id is HtmlTagId.Script or HtmlTagId.Style)
                    {
                        if (tag.IsEndTag) suppressed = Math.Max(0, suppressed - 1);
                        else if (!tag.IsEmptyElement) suppressed++;
                        break;
                    }

                    if (suppressed > 0)
                        break;

                    if (tag is { Id: HtmlTagId.A, IsEndTag: false })
                    {
                        var href = tag.Attributes
                            .FirstOrDefault(attribute => attribute.Id == HtmlAttributeId.Href)
                            ?.Value;

                        if (!string.IsNullOrWhiteSpace(href) && !href.StartsWith('#'))
                            text.Append($" <{href.Trim()}> ");
                    }

                    if (tag.Id is HtmlTagId.Br or HtmlTagId.P or HtmlTagId.Div or HtmlTagId.LI
                        or HtmlTagId.TR or HtmlTagId.H1 or HtmlTagId.H2 or HtmlTagId.H3)
                        text.AppendLine();

                    break;

                case HtmlTokenKind.Data when suppressed == 0:
                    text.Append(((HtmlDataToken)token).Data);
                    break;
            }
        }

        return WebUtility.HtmlDecode(text.ToString());
    }

    /// <summary>
    /// Removes everything from the first sign of a quoted message onwards, then trims a trailing
    /// signature.
    /// </summary>
    /// <remarks>
    /// This is the most security-sensitive function in the inbound path, and it is worth being
    /// plain about why. The invitation a participant received names the person they drew. If they
    /// reply to it rather than using the button, that text is quoted into what they send, and
    /// forwarding it would tell the one person who must not know it. The button is a mailto: for
    /// exactly this reason, so a well-behaved submission has nothing to strip — this is the second
    /// line, for somebody who forwarded their invitation instead.
    ///
    /// It cannot be made reliable across every mail client, and it is not relied upon to be. The
    /// caller separately refuses any message still containing the sender's pick, and the
    /// confirmation echoes back what was stored so a bad cut is visible to the person who wrote it.
    /// </remarks>
    internal static string StripQuotedReplyAndSignature(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        var lines = body.Replace("\r\n", "\n").Split('\n');
        var cut = lines.Length;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();

            if (!QuoteMarker().IsMatch(line)) continue;

            cut = index;
            break;
        }

        var kept = lines.Take(cut).ToList();

        // Anything a client prefixed rather than fenced, plus the blank lines left where the quote
        // used to be.
        while (kept.Count > 0
               && (kept[^1].TrimStart().StartsWith('>') || string.IsNullOrWhiteSpace(kept[^1])))
            kept.RemoveAt(kept.Count - 1);

        var signature = kept.FindLastIndex(line => line.TrimEnd() == "--");

        if (signature >= 0)
            kept = kept.Take(signature).ToList();

        return string.Join("\n", kept).Trim();
    }

    /// <summary>
    /// The line on which a quoted message begins, in the spellings the common clients use.
    /// </summary>
    /// <remarks>
    /// Anchored to the start of a line and deliberately narrow. A pattern loose enough to catch
    /// every variant would also cut a message in half at the first sentence beginning "On", and
    /// silently losing what somebody wrote is worse here than leaving a quote in — the caller
    /// refuses anything that still names the sender's pick either way.
    /// </remarks>
    [GeneratedRegex(
        """
        ^(
            (On\ .{0,200}\ wrote:)
          | (-{2,}\ ?Original\ Message\ ?-{2,})
          | (-{2,}\ ?Forwarded\ message\ ?-{2,})
          | (_{10,})
          | (From:\s.+)
          | (Sent\ from\ my\ .{0,40})
          | (>\s?On\ .{0,200}\ wrote:)
        )\s*$
        """,
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex QuoteMarker();
}
