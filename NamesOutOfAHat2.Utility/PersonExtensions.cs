using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Utility
{
    public static class PersonExtensions
    {
        public static string WriteDisplayName(this Person input) =>
           !string.IsNullOrWhiteSpace(input?.Name ?? string.Empty) ? input!.Name : "Participant";

        public static Participant ToParticipant(this Person input, IList<Recipient> recipients) =>
            new()
            {
                Person = input,
                Recipients = recipients
            };
    }
}
