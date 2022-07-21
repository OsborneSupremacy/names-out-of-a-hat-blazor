
namespace NamesOutOfAHat2.Utility
{
    public static class GenericExtensions
    {
        public static bool In<T>(this T input, params T[] values) =>
            values.Contains(input);
    }
}