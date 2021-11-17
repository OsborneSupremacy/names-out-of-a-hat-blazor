using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public class Hat
    {
        public string Name { get; set; }

        public string EmailTemplate { get; set; }

        [Required, MinLength(1)]
        public IList<Participant>? Participants { get; set; }
    }
}
