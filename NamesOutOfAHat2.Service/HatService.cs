using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Service;

[ServiceLifetime(ServiceLifetime.Scoped)]
public class HatService
{
    public Hat AddParticipant(Hat hatIn, Person person)
    {
        var newRecipient = new Recipient { Person = person, Eligible = true };

        var participantsOut = new List<Participant>();
        var newParticipantRecipients = new List<Recipient>();

        // make the participant a recipient for all existing participants
        foreach (Participant participant in hatIn.Participants)
        {
            // new recipient is recipient for existing participanta
            participantsOut.Add(participant with
            {
                Recipients = participant.Recipients.Concat([newRecipient]).ToList()
            });

            // existing participant is recipient for new participant
            newParticipantRecipients.Add(new Recipient
            {
                Person = participant.Person,
                Eligible = true
            });
        }

        participantsOut.Add(new Participant
        {
            Person = person,
            Recipients = newParticipantRecipients,
            PickedRecipient = Persons.Empty
        });

        return hatIn with
        {
            Participants = participantsOut
        };
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

        // if the person being removed is the organizer, remove the organizer
        Participant organizerOut = hatIn.Organizer.Person.Id == id ? Participants.Empty : hatIn.Organizer;

        return hatIn with
        {
            Organizer = organizerOut,
            Participants = participantsOut
        };
    }

    /// <summary>
    /// The hatIn from local storage isn't exactly the same as the hatIn that was saved
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
                // any old recipients not found in the list of people will be lost
            }

            participantsOut.Add(participant with { Recipients = recipientsOut });
        }

        return hatIn with { Participants = participantsOut };
    }
}
