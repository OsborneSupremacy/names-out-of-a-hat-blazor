namespace GiftExchange.Library.Messaging;

public record VerifyOrganizerRequest
{
    public required string OrganizerEmail { get; init; }

    public required Guid HatId { get; init; }

    public required string VerificationCode { get; init; }
}
