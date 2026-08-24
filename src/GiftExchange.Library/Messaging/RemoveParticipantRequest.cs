namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record RemoveParticipantRequest : IOrganizerScopedRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    public required string Email { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
