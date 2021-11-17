using NamesOutOfAHat2.Model;
using Bogus;
using NamesOutOfAHat2.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class HatShakerService
    {
        public (bool isValid, List<string> errors, Hat hat) ShakeMultiple(Hat hat, int count)
        {
            var randomSeeds = new List<int>();
            for (int x = 0; x <= count; x++)
                randomSeeds.Add(new Randomizer().Int());

            return ShakeMultiple(hat, randomSeeds);
        }

        public (bool isValid, List<string> errors, Hat hat) ShakeMultiple(Hat hat, List<int> randomSeeds)
        {
            bool isValid = false;
            var errors = new List<string>();

            foreach(var seed in randomSeeds)
            {
                System.Diagnostics.Debug.WriteLine(seed);

                (isValid, errors, hat) = Shake(hat, seed);
                if (isValid)
                    break;
            }

            return (isValid, errors, hat);
        }

        public (bool isValid, List<string> errors, Hat hat) Shake(Hat hat, int randomSeed)
        {
            Randomizer.Seed = new Random(randomSeed);
            var faker = new Faker();

            var participants = hat.Participants.ToList();
            var pickedList = new HashSet<Guid>();

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

                if(!eligibleRecipients.Any())
                {
                    errors.Add($"Could not find an eligble recipient for {participant.Person.Name} that was not already taken");
                    break;
                }

                participant.PickedRecipient = faker.PickRandom(eligibleRecipients).Person;
                pickedList.Add(participant.PickedRecipient.Id);
            }

            if(errors.Any())
                participants.ForEach(x => x.PickedRecipient = null);

            return (!errors.Any(), errors, hat);
        }
    }
}
