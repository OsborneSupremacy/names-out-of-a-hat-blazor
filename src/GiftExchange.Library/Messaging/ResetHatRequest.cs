namespace GiftExchange.Library.Messaging;

/// <summary>
/// A request to take a gift exchange back to the state it was in before anybody set it up: the same
/// people, everyone allowed to draw everyone, and nobody holding a name.
/// </summary>
internal record ResetHatRequest : IOrganizerScopedRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
