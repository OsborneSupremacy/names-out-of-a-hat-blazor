namespace NamesOutOfAHat2.Model.DomainModels;

public record OrganizerRegistration
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    public required string VerificationCode { get; init; }

    public required bool Verified { get; init; }
}
