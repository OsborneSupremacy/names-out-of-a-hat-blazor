using LanguageExt.Common;
using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Interface
{
    public interface IDuplicateCheckService
    {
        public Result<bool> Execute(IList<Person> people);
    }
}