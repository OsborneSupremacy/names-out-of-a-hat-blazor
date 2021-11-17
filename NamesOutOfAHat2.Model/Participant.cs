using System.ComponentModel.DataAnnotations;

namespace NamesOutOfAHat2.Model
{
    public record Participant
    {
        public Participant()
        {
        }

        public Participant(Person person)
        {
            Person = person;
        }

        [Required]
        public Person Person { get; set; } = default!;

        [Required, MinLength(1)]
        public IList<Recipient> Recipients { get; set; } = default!;
    }
}
