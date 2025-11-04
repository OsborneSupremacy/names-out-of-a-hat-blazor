namespace GiftExchange.Library.Messaging;

public record CreateHatRequest
{
    public required string HatName { get; init; }

    public required string OrganizerName { get; init; }

    public required string OrganizerEmail { get; init; }
}
