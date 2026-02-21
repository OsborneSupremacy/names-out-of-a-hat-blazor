using Bogus;

namespace GiftExchange.Library.Extensions;

internal static class StringExtensions
{
    extension(string input)
    {
        public string TrimNullSafe()
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return input.Trim();
        }

        public bool ContentEquals(string value) =>
            input.TrimNullSafe().Equals(value.TrimNullSafe(), StringComparison.OrdinalIgnoreCase);

        public string GetPersonEmojiFor()
        {
            Randomizer.Seed = new Random(input.GetHashCode() + input[..1].GetHashCode());
            return new Faker().PickRandom(PersonEmojis);
        }
    }

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
}
