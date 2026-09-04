namespace GiftExchange.Library.Messaging;

/// <summary>
/// The refusals to record when somebody leaves a gift exchange.
/// </summary>
/// <remarks>
/// One request rather than three calls, so that the three lists are written under a single
/// transaction and a leaver cannot end up blocked from one thing and not the other because a
/// connection dropped between two of them.
///
/// The exchange-scoped refusal has no flag: it is written every time, because leaving an exchange
/// is itself the statement that they do not want to be in it. The other two are choices, and they
/// default to false the way an unticked checkbox does.
/// </remarks>
[UsedImplicitly]
internal record RecordDoNotAddRequest
{
    /// <summary>The address refusing. Normalized by the provider, not by the caller.</summary>
    public required string Email { get; init; }

    /// <summary>The exchange being left, always recorded.</summary>
    public required Guid HatId { get; init; }

    /// <summary>The organizer of that exchange, used only when <see cref="BlockOrganizer"/> is set.</summary>
    public required string OrganizerEmail { get; init; }

    /// <summary>Never let this organizer add them to anything again.</summary>
    public required bool BlockOrganizer { get; init; }

    /// <summary>Never let anybody add them to anything again.</summary>
    public required bool BlockAnywhere { get; init; }
}
