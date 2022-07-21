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

        public static Participant ToParticipant(this Person input) => new(input);

        public static Participant AddRecipient(this Participant input, Person person, bool eligible)
        {
            input.Recipients ??= new List<Recipient>();
            input.Recipients.Add(new Recipient()
            {
                Person = person,
                Eligible = eligible
            });
            return input;
        }

        public static Participant Eligible(this Participant input, params Person[] people)
        {
            foreach (var person in people)
                input.AddRecipient(person, true);
            return input;
        }

        public static Participant Ineligible(this Participant input, params Person[] people)
        {
            foreach (var person in people)
                input.AddRecipient(person, false);
            return input;
        }
    }
}