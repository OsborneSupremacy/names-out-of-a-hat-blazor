using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public class Hat
    {
        [Required, MinLength(1)]
        public IList<Participant>? Participants { get; set; }
    }
}
