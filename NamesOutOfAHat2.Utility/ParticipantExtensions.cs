using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Utility;

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

    public static Participant ToParticipant(this Person input)
    {
        return new Participant
        {
            Person = input,
            PickedRecipient = Persons.Empty,
            Recipients = []
        };
    }

    private static Participant AddRecipient(
        this Participant participantIn,
        Person person,
        bool eligible
        ) =>
        participantIn with
        {
            Recipients = participantIn.Recipients.Concat(new List<Recipient>
            {
                new()
                {
                    Person = person,
                    Eligible = eligible
                }
            }).ToList()
        };

    public static Participant AddEligibleRecipients(this Participant input, params Person[] people)
    {
        foreach (var person in people)
            input.AddRecipient(person, true);
        return input;
    }

    public static Participant AddIneligibleRecipients(this Participant input, params Person[] people)
    {
        foreach (var person in people)
            input.AddRecipient(person, false);
        return input;
    }
}
