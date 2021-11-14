using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public record Participant
    {
        [Required]
        public Person Person { get; set; } = default!;

        [Required, MinLength(1)]
        public IList<Recipient> Recipients { get; set; } = default!;
    }
}
