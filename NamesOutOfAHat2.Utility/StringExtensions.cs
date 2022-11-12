using System.Text;
using Bogus;

namespace NamesOutOfAHat2.Utility;

public static class StringExtensions
{
    public static string TrimNullSafe(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input.Trim();
    }

    public static bool ContentEquals(this string input, string value) =>
        input.TrimNullSafe().Equals(value.TrimNullSafe(), StringComparison.OrdinalIgnoreCase);

    public static string ToNaturalLanguageList(this IEnumerable<string> input)
    {
        var s = new StringBuilder();

        var values = input.ToList();

        for (int x = 0; x < values.Count; x++)
        {
            if (x > 0)
            {
                if (x == (values.Count - 1))
                    s.Append(" or ");
                else
                    s.Append(", ");
            }
            s.Append(values[x]);
        }

        return s.ToString();
    }

    public static bool ToBoolOrDefault(this string? input, bool defaultValue)
    {
        if (bool.TryParse(input, out var result)) return result;
        return defaultValue;
    }

    private static readonly List<string> _personEmojis = new() {
        "😀",
        "😁",
        "😆",
        "🤣",
        "🥰",
        "🤩",
        "😺",
        "😸",
        "🤖",
        "😂",
        "🤠",
        "🥳",
        "😅",
        "😉",
        "🤪",
        "😏",
        "😼",
        "🌝",
        "🌞",
        "😎"
    };

    public static string GetPersonEmojiFor(this string input)
    {
        Randomizer.Seed = new Random(input.GetHashCode() + input[..1].GetHashCode());
        return new Faker().PickRandom(_personEmojis);
    }
}
