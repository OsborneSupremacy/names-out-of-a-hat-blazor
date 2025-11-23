using Bogus;

namespace GiftExchange.Library.Extensions;

internal static class StringExtensions
{
    public static string TrimNullSafe(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input.Trim();
    }

    public static bool ContentEquals(this string input, string value) =>
        input.TrimNullSafe().Equals(value.TrimNullSafe(), StringComparison.OrdinalIgnoreCase);

    private static readonly List<string> PersonEmojis =
    [
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
    ];

    public static string GetPersonEmojiFor(this string input)
    {
        Randomizer.Seed = new Random(input.GetHashCode() + input[..1].GetHashCode());
        return new Faker().PickRandom(PersonEmojis);
    }
}
