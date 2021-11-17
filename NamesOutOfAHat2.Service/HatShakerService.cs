using NamesOutOfAHat2.Model;
using Bogus;

namespace NamesOutOfAHat2.Service
{
    public class HatShakerService
    {
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
