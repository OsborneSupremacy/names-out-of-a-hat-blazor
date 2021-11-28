using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public class Hat
    {
        public string? Name { get; set; } = default!;

        [MaxLength(10000)]
        public string? AdditionalInformation { get; set; } = default!;

        [MaxLength(255)]
        public string? PriceRange { get; set; } = default!;

        public Participant? Organizer { get; set;  }

        [Required, 
            MinLength(3, ErrorMessage = "A gift exchange like this needs at least three people"), 
            MaxLength(30, ErrorMessage = "30 is the maximum number of gift exchange participants.")
        ]
        public IList<Participant>? Participants { get; set; }
    }
}
