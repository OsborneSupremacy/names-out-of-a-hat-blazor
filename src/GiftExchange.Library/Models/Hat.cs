namespace GiftExchange.Library.Models;

public record Hat
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required string AdditionalInformation { get; init; }

    public required string PriceRange { get; init; }

    public required Person Organizer { get; init; }

    public required ImmutableList<Participant> Participants { get; init; }

    public required bool InvitationsQueued { get; init; }

    public required DateTimeOffset InvitationsQueuedDate { get; init; }
}

internal static class Hats
{
    public static Hat Empty => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        Status = HatStatus.InProgress,
        AdditionalInformation = string.Empty,
        PriceRange = string.Empty,
        Organizer = Persons.Empty,
        Participants = [],
        InvitationsQueued = false,
        InvitationsQueuedDate = DateTimeOffset.MinValue
    };
}

