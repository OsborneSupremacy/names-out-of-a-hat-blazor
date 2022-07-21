using System.Diagnostics;
using Bogus;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
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

        public Result<Hat> ShakeMultiple(Hat hat, List<int> randomSeeds)
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

        public Result<Hat> Shake(Hat hat, int randomSeed)
        {
            Randomizer.Seed = new Random(randomSeed);
            var faker = new Faker();

            var participants = hat.Participants.ToList();
            var pickedList = new System.Collections.Generic.HashSet<Guid>();

            var errors = new List<string>();

            // clear all existing picked recipients
            participants.ForEach(x => x.PickedRecipient = null);

            for (int x = 1; x <= participants.Count; x++)
            {
                var participant = faker.PickRandom(participants.Where(x => x.PickedRecipient is null));
                var eligibleRecipients = participant.Recipients
                    .Where(x => x.Eligible)
                    .Where(x => !pickedList.Contains(x.Person.Id))
                    .ToList();

                if (!eligibleRecipients.Any())
                {
                    errors.Add($"Could not find an eligible recipient for {participant.Person.Name} that was not already taken");
                    break;
                }

                participant.PickedRecipient = faker.PickRandom(eligibleRecipients).Person;
                pickedList.Add(participant.PickedRecipient.Id);
            }

            if (!errors.Any())
                return hat;

            participants.ForEach(x => x.PickedRecipient = null);
            return new Result<Hat>(new MultiException(errors));
        }
    }
}