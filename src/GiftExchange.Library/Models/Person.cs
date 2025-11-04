using System.ComponentModel.DataAnnotations;

namespace GiftExchange.Library.Models;

public record Person
{
    [Required]
    public required Guid Id { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Name { get; init; }

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public required string Email { get; init; }
}

public static class Persons
{
    public static Person Empty => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        Email = string.Empty
    };
}
