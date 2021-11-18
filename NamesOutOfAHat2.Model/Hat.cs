using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public class Hat
    {
        public string Name { get; set; }

        [MaxLength(255)]
        public string PriceRange { get; set; }

        public Participant? Organizer { get; set;  }

        [Required, MinLength(1)]
        public IList<Participant>? Participants { get; set; }
    }
}
