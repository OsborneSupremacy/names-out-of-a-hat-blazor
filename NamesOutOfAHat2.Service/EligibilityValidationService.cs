using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class EligibilityValidationService
    {
        public (bool isValid, IList<string> errors) Validate(Hat hat)
        {
            var people = hat.Participants.Select(x => x.Person).ToList();
            var errors = new List<string>();

            foreach (var person in people)
            {
                if(!hat.Participants
                    .Where(x => x.Person.Id != person.Id)
                    .SelectMany(x => x.Recipients)
                    .Where(x => x.Eligible && x.Person.Id == person.Id)
                    .Any())
                    errors.Add($"{person.Name} is not an eligible recipient for any participant. Their name will not be picked.");
            }

            return (!errors.Any(), errors);
        }
    }
}
