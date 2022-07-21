using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class EligibilityValidationService
    {
        public Result<bool> Validate(Hat hat)
        {
            var errors = new List<string>();

            var participants = hat.Participants?.ToList() ?? Enumerable.Empty<Participant>().ToList();

            foreach (var participant in participants)
            {
                if (!(participant.Recipients?.Any() ?? false))
                {
                    errors.Add($"{participant.Person.Name} has no possible recipients");
                    continue;
                }
                if (!(participant.Recipients?.Any(x => x.Eligible) ?? false))
                {
                    errors.Add($"{participant.Person.Name} has no eligible recipients");
                    continue;
                }
            }

            if (errors.Any())
                return new Result<bool>(new MultiException(errors));

            var people = participants.Select(x => x.Person).ToList();

            foreach (var person in people)
            {
                if (!participants
                    .Where(x => x.Person.Id != person.Id)
                    .SelectMany(x => x.Recipients)
                    .Where(x => x.Eligible && x.Person.Id == person.Id)
                    .Any())
                    errors.Add($"{person.Name} is not an eligible recipient for any participant. Their name will not be picked.");
            }

            if (errors.Any())
                return new Result<bool>(new MultiException(errors));

            return true;
        }
    }
}