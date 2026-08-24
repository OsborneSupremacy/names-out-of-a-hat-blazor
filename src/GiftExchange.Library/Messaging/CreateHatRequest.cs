namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record CreateHatRequest : IOrganizerScopedRequest
{
    public required string HatName { get; init; }

    public required string OrganizerName { get; init; }

    public required string OrganizerEmail { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
