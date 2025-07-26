using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Utility;

public static class PersonExtensions
{
    public static string WriteDisplayName(this Person input) =>
       !string.IsNullOrWhiteSpace(input?.Name ?? string.Empty) ? input!.Name : "Participant";

    public static Participant ToParticipant(this Person input, List<Recipient> recipients) =>
        new()
        {
            Person = input,
            PickedRecipient = Persons.Empty,
            Recipients = recipients
        };
}
