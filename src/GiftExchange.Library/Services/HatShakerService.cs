using System.Diagnostics;
using Bogus;

namespace GiftExchange.Library.Services;

internal static class HatShakerService
{
    public static (bool success, ImmutableList<Participant> participantsOut) ShakeMultiple(ImmutableList<Participant> participantsIn, int count)
    {
        var randomSeeds = new List<int>();

        for (int x = 0; x <= count; x++)
            randomSeeds.Add(new Randomizer().Int());

        return ShakeMultiple(participantsIn, randomSeeds);
    }

    private static (bool success, ImmutableList<Participant> participantsOut) ShakeMultiple(ImmutableList<Participant> participantsIn, List<int> randomSeeds)
    {
        foreach (var seed in randomSeeds)
        {
#if DEBUG
            Debug.WriteLine(seed);
#endif
            var (success, participantsOut) = Shake(participantsIn, seed);
            if (success)
                return (true, participantsOut);
        }

        return (false, []);
    }

    private static (bool success, ImmutableList<Participant> participantsOut) Shake(ImmutableList<Participant> participantsIn, int randomSeed)
    {
        Randomizer.Seed = new Random(randomSeed);
        var faker = new Faker();

        var participantsOut = new List<Participant>();

        var pickedList = new HashSet<Guid>();

        var errors = new List<string>();

        for (int x = 1; x <= participantsIn.Count; x++)
        {
            var participantIn = faker.PickRandom(participantsIn.Where(p => p.PickedRecipient == Persons.Empty));

            var eligibleRecipients = participantIn.Recipients
                .Where(r => r.Eligible)
                .Where(r => !pickedList.Contains(r.Person.Id))
                .ToList();

            if (!eligibleRecipients.Any())
            {
                errors.Add($"Could not find an eligible recipient for {participantIn.Person.Name} that was not already taken");
                break;
            }

            var participantOut = participantIn with
            {
                PickedRecipient = faker.PickRandom(eligibleRecipients).Person
            };

            participantsOut.Add(participantOut);
            pickedList.Add(participantOut.PickedRecipient.Id);
        }

        if (!errors.Any())
            return (true, participantsOut.ToImmutableList());

        return (false, []);
    }
}
