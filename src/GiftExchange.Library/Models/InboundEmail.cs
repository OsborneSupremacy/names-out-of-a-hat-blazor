namespace GiftExchange.Library.Models;

/// <summary>
/// A received message reduced to the parts that decide what happens to it.
/// </summary>
public record InboundEmail
{
    /// <summary>The address in the From header, lower-cased. Checked against the participant's own.</summary>
    public required string From { get; init; }

    /// <summary>
    /// The message text with quoted replies and signatures removed. Plain text throughout: nothing
    /// the sender wrote is ever forwarded as markup.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Names of files that came with the message and are not being forwarded. Inline images are not
    /// counted — a logo in somebody's signature is not an attachment they meant to send, and
    /// telling them one was dropped would only confuse them.
    /// </summary>
    public required ImmutableList<string> AttachmentNames { get; init; }

    /// <summary>
    /// Whether the message announces itself as machine-generated. An out of office arriving at a
    /// gift ideas address would otherwise be stored as somebody's gift ideas.
    /// </summary>
    public required bool IsAutomated { get; init; }
}

internal static class InboundEmails
{
    public static InboundEmail Empty => new()
    {
        From = string.Empty,
        Body = string.Empty,
        AttachmentNames = [],
        IsAutomated = false
    };
}
