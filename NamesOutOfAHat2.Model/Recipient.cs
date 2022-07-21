using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public record Recipient
    {
        public Recipient()
        {
        }

        public Recipient(Person person, bool eligible)
        {
            Person = person;
            Eligible = eligible;
        }

        [Required]
        public Person Person { get; set; } = default!;

        [Required]
        public bool Eligible { get; set; }
    }
}