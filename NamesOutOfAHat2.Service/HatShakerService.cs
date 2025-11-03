using System.Diagnostics;
using Bogus;
using NamesOutOfAHat2.Model.DomainModels;
using Person = Bogus.Person;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace NamesOutOfAHat2.Service;

[ServiceLifetime(ServiceLifetime.Scoped)]
public class HatShakerService
{
    public Result<Hat> ShakeMultiple(Hat hat, int count)
    {
        var randomSeeds = new List<int>();

        for (int x = 0; x <= count; x++)
            randomSeeds.Add(new Randomizer().Int());

        return ShakeMultiple(hat, randomSeeds);
    }

    private Result<Hat> ShakeMultiple(Hat hat, List<int> randomSeeds)
    {
        var hatOut = new Result<Hat>();

        foreach (var seed in randomSeeds)
        {
#if DEBUG
            Debug.WriteLine(seed);
#endif
            hatOut = Shake(hat, seed);
            if (hatOut.IsSuccess)
                return hatOut;
        }

        return hatOut;
    }

    public Result<Hat> Shake(Hat hatIn, int randomSeed)
    {
        Randomizer.Seed = new Random(randomSeed);
        var faker = new Faker();

        var participantsIn = hatIn.Participants
            .Select(p => p with { PickedRecipient = Persons.Empty }) // clear any previous picks
            .ToList();

        var participantsOut = new List<Participant>();

        var pickedList = new System.Collections.Generic.HashSet<Guid>();

        var errors = new List<ValidationException>();

        for (int x = 1; x <= participantsIn.Count; x++)
        {
            var participantIn = faker.PickRandom(participantsIn.Where(p => p.PickedRecipient is null));
            var eligibleRecipients = participantIn.Recipients
                .Where(r => r.Eligible)
                .Where(r => !pickedList.Contains(r.Person.Id))
                .ToList();

            if (!eligibleRecipients.Any())
            {
                errors.Add(new($"Could not find an eligible recipient for {participantIn.Person.Name} that was not already taken"));
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
            return hatIn with
            {
                Participants = participantsOut
            };

        return new Result<Hat>(new AggregateException(errors));
    }
}
