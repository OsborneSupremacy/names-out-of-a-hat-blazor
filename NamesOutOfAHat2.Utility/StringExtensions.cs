namespace NamesOutOfAHat2.Utility
{
    public static class StringExtensions
    {
        public static string TrimNullSafe(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return input.Trim();
        }

        public static bool ContentEquals(this string input, string value) =>
            input.TrimNullSafe().Equals(value.TrimNullSafe(), StringComparison.OrdinalIgnoreCase);
    }
}
