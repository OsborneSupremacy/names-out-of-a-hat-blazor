namespace GiftExchange.Library.Models;

public record Person
{
    public required string Name { get; init; }

    public required string Email { get; init; }
}

internal static class Persons
{
    public static Person Empty => new()
    {
        Name = string.Empty,
        Email = string.Empty
    };

    public static Person Reacted => new() { Name = "Hidden", Email = "someone@somedomain.com" };
}
