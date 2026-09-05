namespace GiftExchange.Library.Models;

public record Hat
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required string AdditionalInformation { get; init; }

    public required string PriceRange { get; init; }

    public required Person Organizer { get; init; }

    public required ImmutableList<Participant> Participants { get; init; }

    public required DateTimeOffset InvitationsQueuedDate { get; init; }
}

internal static class Hats
{
    public static Hat Empty => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        Status = HatStatus.InProgress,
        AdditionalInformation = string.Empty,
        PriceRange = string.Empty,
        Organizer = Persons.Empty,
        Participants = [],
        InvitationsQueuedDate = DateTimeOffset.MinValue
    };

    /// <summary>
    /// The face worn in this hat by the participant of that name, or the empty string when nobody
    /// in it is called that.
    /// </summary>
    /// <remarks>
    /// Looked up by name because that is what a pick is by the time it reaches a domain record —
    /// <c>Participant.PickedRecipient</c> is a name, not an id — and names are unique within a hat,
    /// which <c>AddParticipantService</c> is what enforces.
    ///
    /// Empty rather than a stand-in face for the misses, and the misses are real: an unshaken
    /// participant has drawn nobody, and the detail view is served a draw redacted to "Hidden"
    /// until the exchange is closed. Somewhere that has no face to show should show none.
    /// </remarks>
    public static string EmojiFor(this Hat hat, string participantName)
    {
        if (string.IsNullOrWhiteSpace(participantName)) return string.Empty;

        return hat.Participants
            .Where(participant => participant.Person.Name.ContentEquals(participantName))
            .Select(participant => participant.Emoji)
            .FirstOrDefault(string.Empty);
    }
}
