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
                ep.Eligible(person);
                // existing participant is recipient for new person
                newGiverRecipients.Add(new Recipient(ep.Person, true));
            }

            return hat
                .AddParticipant(person.ToParticipant(newGiverRecipients));
        }

        public Hat RemoveParticipant(Hat hat, Participant participant)
        {
            var id = participant.Person.Id;

            var participants = hat.Participants?.ToList() ?? Enumerable.Empty<Participant>().ToList();

            foreach (var parcipant in participants)
            {
                var recipients = parcipant.Recipients.ToList();
                recipients.RemoveAll(x => x.Person.Id == id);
                parcipant.Recipients = recipients;
            }

            participants.RemoveAll(x => x.Person.Id == id);
            hat.Participants = participants;

            // if person being removed is organizer, remove the organizer
            if ((hat.Organizer?.Person.Id ?? Guid.Empty) == id)
                hat.Organizer = null;

            return hat;
        }

        /// <summary>
        /// the hat from local storage isn't exactly the same as the hat that was saved
        /// because through serialization / deserialization, participant people are no
        /// longer the same objects as recipient people.
        /// To get them to be the same object, rebuild recipient lists
        /// </summary>
        /// <returns></returns>
        public Hat ReconstructParticipants(Hat hat)
        {
            var participantPeople = hat!.Participants!.ToDictionary(x => x.Person.Id, x => x.Person);

            foreach (var partcipant in hat!.Participants!)
            {
                var newRecips = new List<Recipient>();

                foreach (var oldRecip in partcipant.Recipients)
                {
                    // old recipient found
                    if (participantPeople.TryGetValue(oldRecip.Person.Id, out var newRecip))
                        newRecips.Add(new Recipient(newRecip, oldRecip.Eligible));
                    // any old recipients not found in list of people will be lost
                }

                partcipant.Recipients = newRecips;
            }

            return hat;
        }
    }
}
