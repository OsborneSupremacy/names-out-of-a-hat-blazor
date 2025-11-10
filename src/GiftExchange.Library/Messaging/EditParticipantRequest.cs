namespace GiftExchange.Library.Messaging;

// ReSharper disable once ClassNeverInstantiated.Global
public record EditParticipantRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    public required Guid PersonId { get; init; }

    public required ImmutableList<Guid> EligibleRecipientIds { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }
}
