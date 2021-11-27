using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Utility
{
    public static class HatExtensions
    {
        public static Hat AddParticipant(this Hat input, Participant participant)
        {
            input.Participants ??= new List<Participant>();
            input.Participants.Add(participant);
            return input;
        }
    }
}
