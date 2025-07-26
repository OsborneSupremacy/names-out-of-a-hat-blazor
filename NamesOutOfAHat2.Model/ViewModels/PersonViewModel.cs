namespace NamesOutOfAHat2.Model.ViewModels;

public record PersonViewModel
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public static implicit operator Person(PersonViewModel vm) => new Person
    {
        Id = vm.Id,
        Name = vm.Name,
        Email = vm.Email
    };
}
