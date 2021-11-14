using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public record Recipient
    {
        [Required]
        public Person Person { get; set; } = default!;

        [Required]
        public bool Eligible { get; set; }
    }
}
