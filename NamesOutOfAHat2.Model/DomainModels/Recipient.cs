using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model.DomainModels;

public record Recipient
{
    [Required]
    public required Person Person { get; init; }

    [Required]
    public required bool Eligible { get; init; }

    public static implicit operator Recipient(RecipientViewModel vm) => new Recipient
    {
        Person = vm.Person,
        Eligible = vm.Eligible
    };
}

