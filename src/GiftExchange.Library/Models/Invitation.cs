namespace GiftExchange.Library.Models;

public record Invitation
{
    public required string RecipientEmail { get; init; }

    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }
}
