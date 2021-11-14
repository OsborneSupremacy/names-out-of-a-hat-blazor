using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
