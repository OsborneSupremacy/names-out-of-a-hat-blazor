using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamesOutOfAHat2.Model
{
    public class Hat
    {
        [Required, MinLength(1)]
        public IList<Participant>? Participants { get; set; }
    }
}
