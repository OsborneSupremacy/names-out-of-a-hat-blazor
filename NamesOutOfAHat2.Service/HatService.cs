using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class HatService
    {
        public Hat AddParticipant(Hat hat, Person person)
        {
            hat.Participants ??= new List<Participant>();

            var newGiverRecipients = new List<Recipient>();

            // make person a recipient for all existing participants
            foreach (Participant ep in hat.Participants)
            {
                // new person is recipient for existing participant
                ep.Recipients.Add(new Recipient(person, true));
                // existing participant is recipient for new person
                newGiverRecipients.Add(new Recipient(ep.Person, true));
            }

            // add new participant to hat
            var participant = new Participant(person)
            {
                Recipients = newGiverRecipients
            };

            hat.Participants.Add(participant);

            return hat;
        }
    }
}
