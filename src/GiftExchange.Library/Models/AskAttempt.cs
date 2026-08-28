namespace GiftExchange.Library.Models;

/// <summary>
/// What happened when one person on an asker's list was asked.
/// </summary>
/// <remarks>
/// One of these per name chosen, kept in the order they were offered, because both the page and the
/// email that report a round have to name people individually. "We asked 2 of 3" is not something
/// anybody can act on; "we asked Dad and Sarah, but you already asked Bob on 3 August" is.
/// </remarks>
public record AskAttempt
{
    public required string Name { get; init; }

    public required bool Sent { get; init; }

    /// <summary>
    /// When this asker last asked this person, on an attempt the throttle refused.
    /// <see cref="DateTimeOffset.MinValue"/> when it was sent, and also when the earlier date could
    /// not be read back — in which case the wording drops the date rather than inventing one.
    /// </summary>
    public required DateTimeOffset PreviouslyAskedAt { get; init; }
}
