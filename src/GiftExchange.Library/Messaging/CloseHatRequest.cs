namespace GiftExchange.Library.Messaging;

internal record CloseHatRequest : IOrganizerScopedRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
