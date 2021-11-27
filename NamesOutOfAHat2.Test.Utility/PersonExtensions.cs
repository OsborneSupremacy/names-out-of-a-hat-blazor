using NamesOutOfAHat2.Model;
using System.Diagnostics.CodeAnalysis;

namespace NamesOutOfAHat2.Test.Utility
{
    [ExcludeFromCodeCoverage]
    public static class PersonExtensions
    {
        public static Person ToPerson(this string name) =>
            new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = $"{name}@gmail.com"
            };
    }
}
