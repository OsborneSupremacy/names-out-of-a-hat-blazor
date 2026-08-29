namespace GiftExchange.Library.Messaging;

/// <summary>
/// What a refusal has to say: which rule the message ran into, and where the next attempt should be
/// sent.
/// </summary>
/// <remarks>
/// The address is what makes this a record rather than three more parameters. Every message this
/// application sends comes from the no-reply address, so a refusal that does not carry a way back
/// is a dead end — and the way back is not the same for the two kinds of submission, so it cannot
/// be a constant either.
/// </remarks>
[UsedImplicitly]
public record ComposeRejectionRequest
{
    public required GiftIdeaSubmissionOutcome Outcome { get; init; }

    /// <summary>Anything the sender attached, which is named rather than silently dropped.</summary>
    public required ImmutableList<string> DroppedAttachments { get; init; }

    /// <summary>
    /// The token from the address the refused message was sent to, which is also where the next one
    /// should go.
    ///
    /// Carried in the case it arrived in. The token is base64url, so lower-casing it — the ordinary
    /// thing to do with part of an email address — would hand the sender an address that resolves
    /// to nothing.
    /// </summary>
    public required string GiftIdeasToken { get; init; }

    /// <summary>
    /// Whether the refused message was somebody's suggestion about another participant rather than
    /// their own words about themselves, which decides which invitation is worth repeating.
    ///
    /// Passed rather than inferred from the people involved, for the reason
    /// <see cref="GiftIdeaRoute.IsContribution"/> gives.
    /// </summary>
    public required bool IsContribution { get; init; }

    /// <summary>
    /// The person the ideas were about. Read only when <see cref="IsContribution"/> is set, where
    /// the button names them.
    /// </summary>
    public required string SubjectName { get; init; }
}
