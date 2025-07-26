using NamesOutOfAHat2.Model.ViewModels;

namespace NamesOutOfAHat2.Client.Extensions;

public static class PersonViewModelExtensions
{
    public static string WriteDisplayName(this PersonViewModel input) =>
        !string.IsNullOrWhiteSpace(input?.Name ?? string.Empty) ? input!.Name : "Participant";
}
