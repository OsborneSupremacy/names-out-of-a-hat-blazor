namespace GiftExchange.Library.Models;

internal record Hat
{
    [Required]
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string AdditionalInformation { get; init; }

    public required string PriceRange { get; init; }

    public required Person Organizer { get; init; }

    public required ImmutableList<Participant> Participants { get; init; }

    public required bool OrganizerVerified { get; init; }

    public required bool RecipientsAssigned { get; init; }
}

internal static class Hats
{
    public static Hat Empty => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        AdditionalInformation = string.Empty,
        PriceRange = string.Empty,
        Organizer = Persons.Empty,
        Participants = [],
        OrganizerVerified = false,
        RecipientsAssigned = false
    };
}

