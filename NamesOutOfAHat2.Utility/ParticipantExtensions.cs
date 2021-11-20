using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Utility
{
    public static class ParticipantExtensions
    {
        public static string WriteEligibleRecipients(this Participant input) => 
            input
                .Recipients
                .Where(x => x.Eligible)
                .Select(x => x.Person)
                .Select(x => x.Name)
                .OrderBy(x => x)
                .ToNaturalLanguageList();

        public static string WriteDisplayName(this Participant input) =>
           !string.IsNullOrWhiteSpace(input.Person?.Name ?? string.Empty) ? input.Person!.Name : "Participant";
    }
}
