namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record PreviewInvitationsResponse
{
    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }

    /// <summary>
    /// Shown back to the organizer before they send, so it is clear the send is attributable.
    /// </summary>
    public required string SenderIpAddress { get; init; }
}
