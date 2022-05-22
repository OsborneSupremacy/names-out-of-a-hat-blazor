using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Utility;

public static class HatExtensions
{
    public static Hat AddParticipant(this Hat input, Participant participant)
    {
        input.Participants ??= new List<Participant>();
        input.Participants.Add(participant);
        return input;
    }

    public static OrganizerRegistration ToRegistration(this Hat input, string code) =>
        new OrganizerRegistration
        {
            HatId = input.Id,
            OrganizerEmail = input.Organizer?.Person.Email ?? string.Empty,
            VerificationCode = code,
            Verified = false
        };

    public static OrganizerRegistration ToRegistration(this Hat input) =>
        input.ToRegistration(string.Empty);
}
