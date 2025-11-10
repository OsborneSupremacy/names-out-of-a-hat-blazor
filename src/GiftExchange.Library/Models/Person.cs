namespace GiftExchange.Library.Models;

public record Person
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

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
