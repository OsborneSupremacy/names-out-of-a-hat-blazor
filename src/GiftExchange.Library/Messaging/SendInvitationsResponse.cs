namespace GiftExchange.Library.Messaging;

public record SendInvitationsResponse
{
    public required string SuccessMessage { get; init; }

    public required ImmutableList<string> Errors { get; init; }
}
