namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record AssignRecipientsRequest : IOrganizerScopedRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
