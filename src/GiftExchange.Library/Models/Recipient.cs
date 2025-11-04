using System.ComponentModel.DataAnnotations;

namespace GiftExchange.Library.Models;

public record Recipient
{
    [Required]
    public required Person Person { get; init; }

    [Required]
    public required bool Eligible { get; init; }
}

