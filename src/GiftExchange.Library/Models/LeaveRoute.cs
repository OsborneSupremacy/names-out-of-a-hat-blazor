namespace GiftExchange.Library.Models;

/// <summary>
/// Everything the leave pages need resolved before they can act, found from the hash of the token
/// in the link that was followed.
///
/// One lookup rather than several because the pages need all of it or none of it: who is leaving,
/// which exchange, what state that exchange is in, and who has to be told.
/// </summary>
/// <remarks>
/// There is no organizer variant of this record and no flag saying whether the holder is one.
/// Organizers are never issued a leave token, so a token that resolves at all belongs to somebody
/// who may leave — the absence is the check, and a boolean here would invite a caller to forget it.
/// </remarks>
public record LeaveRoute
{
    /// <summary>The leaver's participant row, within <see cref="HatId"/>.</summary>
    public required Guid ParticipantId { get; init; }

    public required Guid HatId { get; init; }

    public required string HatName { get; init; }

    /// <summary>One of <see cref="HatStatuses.All"/>. What happens after the removal turns on it.</summary>
    public required string HatStatus { get; init; }

    /// <summary>The participant leaving.</summary>
    public required Person Leaver { get; init; }

    /// <summary>
    /// Who runs the exchange. Named on the leave page, told by name afterwards, and the scope of
    /// the middle do-not-add list if the leaver asks for it.
    /// </summary>
    public required Person Organizer { get; init; }
}

internal static class LeaveRoutes
{
    public static LeaveRoute Empty => new()
    {
        ParticipantId = Guid.Empty,
        HatId = Guid.Empty,
        HatName = string.Empty,
        HatStatus = string.Empty,
        Leaver = Persons.Empty,
        Organizer = Persons.Empty
    };
}
