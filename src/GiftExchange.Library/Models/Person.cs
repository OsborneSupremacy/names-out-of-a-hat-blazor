namespace GiftExchange.Library.Models;

public record Person
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }
}

public static class Persons
{
    private static readonly Guid RedactedGuid = new("11111111-1111-1111-1111-111111111111");

    public static Person Empty => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        Email = string.Empty
    };

    public static Person Reacted => new() { Id = RedactedGuid, Name = "Hidden", Email = "someone@somedomain.com" };
}
