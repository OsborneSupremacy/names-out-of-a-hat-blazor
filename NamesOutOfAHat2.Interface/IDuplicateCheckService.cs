using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Interface
{
    public interface IDuplicateCheckService
    {
        public (bool duplicatesExist, IList<string> errorMessages) Execute(IList<Person> people);
    }
}
