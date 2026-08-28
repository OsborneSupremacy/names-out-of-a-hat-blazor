namespace GiftExchange.Library.Models;

/// <summary>
/// Somebody an asker has actually chosen, resolved to a person we can mail.
/// </summary>
/// <remarks>
/// Separate from <see cref="AskCandidate"/> only because of the address. A candidate is rendered
/// into a page another participant reads and must not carry one; a target has been chosen and is
/// about to be written to, so it must. Keeping the two apart means the type that reaches the HTML
/// has no address in it to leak, rather than an address that this particular page happens not to
/// print.
/// </remarks>
public record AskTarget
{
    public required Guid ParticipantId { get; init; }

    public required Person Person { get; init; }

    /// <summary>
    /// Whether this is the asker's own pick, which decides which of two quite different emails they
    /// get: "what would you like?" or "what do you think they'd like?".
    /// </summary>
    public required bool IsTheirPick { get; init; }
}
