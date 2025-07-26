using NamesOutOfAHat2.Model.ViewModels;

namespace NamesOutOfAHat2.Client.Extensions;

public static class ParticipantViewModelExtensions
{
    public static string WriteDisplayName(this ParticipantViewModel input) =>
        !string.IsNullOrWhiteSpace(input.Person?.Name ?? string.Empty) ? input.Person!.Name : "Participant";
}
