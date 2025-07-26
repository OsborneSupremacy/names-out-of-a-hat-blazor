using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model.DomainModels;

public record Person
{
    [Required]
    public required Guid Id { get; init; }

    [Required(AllowEmptyStrings = false)]
    public required string Name { get; init; }

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public required string Email { get; init; }

    public static implicit operator PersonViewModel(Person p) => new PersonViewModel
    {
        Id = p.Id,
        Name = p.Name,
        Email = p.Email
    };
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
