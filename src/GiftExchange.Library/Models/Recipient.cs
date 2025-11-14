namespace GiftExchange.Library.Models;

internal record Recipient
{
    [Required]
    public required Person Person { get; init; }

    [Required]
    public required bool Eligible { get; init; }
}

