namespace GiftExchange.Library.Messaging;

internal record InitiateOrganizerVerificationRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }
}
