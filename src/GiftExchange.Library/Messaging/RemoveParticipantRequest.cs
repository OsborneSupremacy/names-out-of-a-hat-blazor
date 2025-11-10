namespace GiftExchange.Library.Messaging;

// ReSharper disable once ClassNeverInstantiated.Global
public record RemoveParticipantRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    public required Guid PersonId { get; init; }
}
