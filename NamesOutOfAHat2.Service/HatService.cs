using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Service;

[ServiceLifetime(ServiceLifetime.Scoped)]
public class HatService
{
    public Hat AddParticipant(Hat hatIn, Person person)
    {
        var newGiverRecipients = new List<Recipient>();

        // make person a recipient for all existing participants
        foreach (Participant participant in hatIn.Participants)
        {
            // new person is recipient for existing participant
            participant.AddEligibleRecipients(person);
            // existing participant is recipient for new person
            newGiverRecipients.Add(new Recipient
            {
                Person = participant.Person,
                Eligible = true
            });
        }

        return hatIn
            .AddParticipant(person.ToParticipant(newGiverRecipients));
    }

    public Hat RemoveParticipant(Hat hatIn, Participant participantIn)
    {
        var id = participantIn.Person.Id;

        var participantsIn = hatIn.Participants;
        var participantsOut = new List<Participant>();

        foreach (var participant in participantsIn.Where(p => p.Person.Id != id))
            participantsOut.Add(participant with
            {
                Recipients = participant.Recipients.Where(r => r.Person.Id != id).ToList()
            });

        // if person being removed is organizer, remove the organizer
        Participant? organizerOut = hatIn.Organizer?.Person.Id == id ? null : hatIn.Organizer;

        return hatIn with
        {
            Organizer = organizerOut ?? Participants.Empty,
            Participants = participantsOut
        };
    }

    /// <summary>
    /// the hatIn from local storage isn't exactly the same as the hatIn that was saved
    /// because through serialization / deserialization, participant people are no
    /// longer the same objects as recipient people.
    /// To get them to be the same object, rebuild recipient lists
    /// </summary>
    /// <returns></returns>
    public Hat ReconstructParticipants(Hat hatIn)
    {
        var participantPeople = hatIn!.Participants!.ToDictionary(x => x.Person.Id, x => x.Person);

        var participantsOut = new List<Participant>();

        foreach (var participant in hatIn!.Participants!)
        {
            var recipientsOut = new List<Recipient>();

            foreach (var recipientIn in participant.Recipients)
            {
                // old recipient found
                if (participantPeople.TryGetValue(recipientIn.Person.Id, out var recipientOut))
                    recipientsOut.Add(recipientIn with { Person = recipientOut });
                // any old recipients not found in list of people will be lost
            }

            participantsOut.Add(participant with { Recipients = recipientsOut });
        }

        return hatIn with { Participants = participantsOut };
    }
}
