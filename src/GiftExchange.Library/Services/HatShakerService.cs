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

        participantsIn = participantsIn
            .Select(p => p with { PickedRecipient = Persons.Empty }) // clear any previous picks
            .ToImmutableList();

        var participantsOut = new List<Participant>();

        var assignedGivers = new HashSet<string>();
        var assignedRecipients = new HashSet<string>();

        var errors = new List<string>();

        foreach (var _ in participantsIn)
        {
            var participantIn = faker.
                PickRandom(
                    participantsIn
                        .Where(p => !assignedGivers.Contains(p.Person.Email))
                        .Where(p => p.PickedRecipient == Persons.Empty)
                );

            var eligibleRecipients = participantIn.Recipients
                .Where(r => r.Eligible)
                .Where(r => !assignedRecipients.Contains(r.Person.Email))
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

            assignedGivers.Add(participantIn.Person.Email);
            assignedRecipients.Add(participantOut.PickedRecipient.Email);
        }

        if (!errors.Any())
            return (true, participantsOut.ToImmutableList());

        return (false, []);
    }
}
