using System.Web;

namespace GiftExchange.Library.Services;

/// <summary>
/// How a run of people's names is written into a sentence.
/// </summary>
/// <remarks>
/// Its own class because both the page reporting a round of asking and the email reporting the same
/// round have to do this, and they are otherwise unrelated. Two private copies would have been
/// shorter to write and would have drifted the first time one of them was reworded, leaving the
/// page and the email describing the same event differently.
/// </remarks>
internal static class NameFormatting
{
    /// <summary>
    /// "Dad", "Dad and Sarah", "Dad, Sarah and Chris".
    /// </summary>
    /// <remarks>
    /// Encodes on the way through, because every name reaching this came from an organizer typing
    /// it into a form and every caller drops the result straight into HTML. Encoding here rather
    /// than at each call site means a new caller cannot forget.
    /// </remarks>
    internal static string ToSentenceList(IEnumerable<string> names)
    {
        var encoded = names.Select(name => HttpUtility.HtmlEncode(name) ?? string.Empty).ToImmutableList();

        return encoded.Count switch
        {
            0 => string.Empty,
            1 => encoded[0],
            _ => $"{string.Join(", ", encoded.Take(encoded.Count - 1))} and {encoded[^1]}"
        };
    }
}
