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
        /// The form an email address is stored and compared in on the do-not-add lists: trimmed and
        /// lower-cased.
        /// </summary>
        /// <remarks>
        /// Normalised on the way in rather than at the point of comparison, so that a check is an
        /// index seek on the column rather than a scan calling <c>lower()</c> on every row. Every
        /// other address column in this schema is compared with <see cref="ContentEquals"/>, which
        /// is fine for a handful of participants held in memory and would not be fine for a list
        /// that grows with everybody who has ever refused an invitation.
        ///
        /// Invariant lower-casing, not the current culture's. The Turkish dotless i turns an
        /// ASCII 'I' into something that no longer matches the address it came from, and an address
        /// that stops matching itself is a block that silently stops blocking.
        /// </remarks>
        public string ToNormalizedEmail() =>
            input.TrimNullSafe().ToLowerInvariant();

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
