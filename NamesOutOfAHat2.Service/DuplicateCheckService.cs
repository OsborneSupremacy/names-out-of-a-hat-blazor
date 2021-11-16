using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
    public abstract class DuplicateCheckService
    {
        protected IList<string> ErrorMessages { get; set; }

        protected HashSet<string> ErrorValues { get; set; }

        protected DuplicateCheckService()
        {
            ErrorMessages = new List<string>();
            ErrorValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        protected void AddErrors<T>(IList<T> duplicateValues, string messageTemplate, Func<T, string> valueFormatter)
        {
            foreach (var value in duplicateValues)
            {
                var valueString = valueFormatter(value);
                // don't add the error value if it already exists
                if (ErrorValues.Contains(valueString)) continue;
                ErrorValues.Add(valueString);
                ErrorMessages.Add(messageTemplate.Replace("{value}", valueString));
            }
        }

        protected static (bool, IList<T> duplicateValues) DuplicatesExist<T>(
            IList<Person> people,
            Func<IList<Person>, IEnumerable<T>> selector,
            Func<Person, T, bool> equals
        )
        {
            var duplicateValues = new List<T>();
            var values = selector(people).ToHashSet();

            foreach (var value in values)
                if (people
                    .Where(x => equals(x, value))
                    .Count() > 1)
                    duplicateValues.Add(value);

            return (duplicateValues.Any(), duplicateValues);
        }
    }

    [RegistrationTarget(typeof(IDuplicateCheckService))]
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class NameDuplicateCheckService : DuplicateCheckService, IDuplicateCheckService
    {
        protected static Func<IList<Person>, IEnumerable<string>> _nameSelector = (IList<Person> people) =>
            people.Select(x => x.Name.TrimNullSafe());

        protected static Func<Person, string, bool> _nameEquals = (Person person, string value) =>
            person.Name.ContentEquals(value);

        public (bool duplicatesExist, IList<string> errorMessages) Execute(IList<Person> people)
        {
            var (duplicatesExist, duplicateValues) = DuplicatesExist(people, _nameSelector, _nameEquals);
            if (!duplicatesExist) return (false, ErrorMessages);

            AddErrors(duplicateValues,
    "The name, `{value}` is associated with more than one person in your gift exchange. That could cause confusion for the participants. Please differentiate between the people named `{value}` (middle/last initial, city, etc.)",
                (string value) => { return value; }
            );

            return (true, ErrorMessages);
        }
    }

    [RegistrationTarget(typeof(IDuplicateCheckService))]
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class EmailDuplicateCheckService : DuplicateCheckService, IDuplicateCheckService
    {
        protected static Func<IList<Person>, IEnumerable<string>> _emailSelector = (IList<Person> people) =>
            people.Select(x => x.Email.TrimNullSafe());

        protected static Func<Person, string, bool> _emailEquals = (Person person, string value) =>
            person.Email.ContentEquals(value);

        public (bool duplicatesExist, IList<string> errorMessages) Execute(IList<Person> people)
        {
            var (duplicatesExist, duplicateValues) = DuplicatesExist(people, _emailSelector, _emailEquals);
            if (!duplicatesExist) return (false, ErrorMessages);

            AddErrors(duplicateValues,
    "The email address, `{value}`, is associated with more than one person in your gift exchange. If multiple names are sent to that address, that's going to cause problems. Everyone needs their own address.",
                (string value) => { return value; }
            );

            return (true, ErrorMessages);
        }
    }

    /// <summary>
    /// IDs are assigned on the frontend. We want to verify that no ID is re-used. This would only
    /// happen if there was a bug on the frontend. Probably not likely, but could yield bad consequences
    /// (e.g. a person being assigned to themself)
    /// </summary>
    [RegistrationTarget(typeof(IDuplicateCheckService))]
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class IdDuplicateCheckService : DuplicateCheckService, IDuplicateCheckService
    {
        protected static Func<IList<Person>, IEnumerable<Guid>> _idSelector = (IList<Person> people) =>
            people.Select(x => x.Id);

        protected static Func<Person, Guid, bool> _idEquals = (Person person, Guid value) =>
            person.Id.Equals(value);

        public (bool duplicatesExist, IList<string> errorMessages) Execute(IList<Person> people)
        {
            var (duplicatesExist, duplicateValues) = DuplicatesExist(people, _idSelector, _idEquals);
            if (!duplicatesExist) return (false, ErrorMessages);

            AddErrors(duplicateValues,
    "We apologize but there in an internal issue with this application.",
                (Guid value) => { return value.ToString("N"); }
            );

            return (true, ErrorMessages);
        }
    }
}
