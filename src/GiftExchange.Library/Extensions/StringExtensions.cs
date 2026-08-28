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

        /// <summary>
        /// The emoji a name is marked with wherever it appears in an email.
        /// </summary>
        /// <remarks>
        /// A Randomizer of its own rather than Bogus's static <c>Randomizer.Seed</c>. Assigning
        /// that seeds every Faker created afterwards in the same process, so composing one email
        /// decided what the next unrelated piece of randomness would be — which in the test suite
        /// showed up as two gift exchanges being generated with the same id.
        ///
        /// The seed is derived from the characters of the name rather than from
        /// <see cref="object.GetHashCode"/>, which is randomised per process. The same person is
        /// named in more than one email and now carries the same emoji in all of them, rather than
        /// changing between the invitation and the message saying the exchange has finished.
        /// </remarks>
        public string GetPersonEmojiFor()
        {
            var seed = input.Aggregate(17, (current, character) => unchecked(current * 31 + character));
            return new Randomizer(seed).ListItem(PersonEmojis);
        }

        public static Guid ToGuidOrEmpty(string value) =>
            Guid.TryParse(value, out var guid) ? guid : Guid.Empty;
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
