namespace NamesOutOfAHat2.Model.ViewModels;

public record RecipientViewModel
{
    public required Person Person { get; init; }

    public required bool Eligible { get; set; }

    public static implicit operator RecipientViewModel(Recipient r) => new RecipientViewModel
    {
        Person = r.Person,
        Eligible = r.Eligible
    };
}
