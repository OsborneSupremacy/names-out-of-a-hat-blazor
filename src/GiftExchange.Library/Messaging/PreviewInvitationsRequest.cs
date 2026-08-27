namespace GiftExchange.Library.Messaging;

[UsedImplicitly]
public record PreviewInvitationsRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    /// <summary>
    /// Taken from the request context by the handler, never from the caller.
    /// </summary>
    public string SenderIpAddress { get; init; } = string.Empty;
}
