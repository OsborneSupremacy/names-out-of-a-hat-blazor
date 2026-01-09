namespace GiftExchange.Library.Messaging;

internal record StartOrganizerVerificationRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }
}
