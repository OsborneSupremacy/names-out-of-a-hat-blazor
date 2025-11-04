namespace GiftExchange.Library.Messaging;

public record EditParticipantResponse
{
    public required string SuccessMessage { get; init; }

    public required ImmutableList<string> Errors { get; init; }
}
