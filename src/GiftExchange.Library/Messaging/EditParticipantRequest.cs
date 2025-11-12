namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record EditParticipantRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public required ImmutableList<string> EligibleRecipientEmails { get; init; }

    public required string Email { get; init; }
}
