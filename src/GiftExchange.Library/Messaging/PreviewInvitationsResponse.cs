namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record PreviewInvitationsResponse
{
    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }
}
