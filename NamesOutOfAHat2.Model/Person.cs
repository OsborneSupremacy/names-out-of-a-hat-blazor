using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model;

public record Person
{
    [Required]
    public Guid Id { get; set; } = default!;

    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = default!;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string Email { get; set; } = default!;
}
