namespace GiftExchange.Library.Messaging;

public record GetParticipantRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    public required string ParticipantEmail { get; init; }

    public required bool ShowPickedRecipients { get; init; }
}
