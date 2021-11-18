
namespace NamesOutOfAHat2.Model
{
    public class ApiKeys : Dictionary<string, string>
    {
        public ApiKeys() : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
