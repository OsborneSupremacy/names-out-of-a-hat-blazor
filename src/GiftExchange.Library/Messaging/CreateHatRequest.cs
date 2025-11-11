namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record CreateHatRequest
{
    public required string HatName { get; init; }

    public required string OrganizerName { get; init; }

    public required string OrganizerEmail { get; init; }
}
