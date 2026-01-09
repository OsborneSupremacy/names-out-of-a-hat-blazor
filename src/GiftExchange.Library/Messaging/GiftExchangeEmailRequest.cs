namespace GiftExchange.Library.Messaging;

internal record GiftExchangeEmailRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    public required string RecipientEmail { get; init; }

    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }
}
