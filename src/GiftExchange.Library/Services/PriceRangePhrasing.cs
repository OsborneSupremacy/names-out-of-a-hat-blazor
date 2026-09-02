using System.Globalization;

namespace GiftExchange.Library.Services;

/// <summary>
/// How the amount an organizer set is put into a sentence in an invitation.
/// </summary>
/// <remarks>
/// The box is labelled "Price Range", but it is free text and organizers write in it whatever
/// describes their exchange. "$25 - $40", yes, but also "around $100", "under $25", "no more than
/// 30 dollars", "no limit", and "Keep it under $20". The line used to be assembled as "Please
/// purchase a gift in the range of {whatever they typed}.", which held up only for the first of
/// those and produced "in the range of around $100." and "in the range of under $25." for the rest.
///
/// No single wording carries all of them, so this does not look for one. It sorts the text into two
/// shapes instead:
///
/// - Text that names an amount: a figure, alone or behind a qualifier such as "around" or "no more
///   than". These go behind "costing", which takes a bare amount, a qualified one and a range
///   equally well, where "in the range of" only ever took a range.
/// - Everything else, which is an organizer writing their own instruction rather than filling in a
///   number. Those are left as they wrote them and only punctuated: a sentence built around such
///   text reads worse than the one they had already written.
/// </remarks>
internal static class PriceRangePhrasing
{
    /// <summary>
    /// Words that can stand in front of a figure and still leave a phrase that "costing" accepts.
    /// </summary>
    /// <remarks>
    /// Only the first word of the phrase is looked up, so multi-word qualifiers appear here as
    /// their opening word alone — "no" for "no more than", "up" for "up to", "at" for "at least".
    /// That is deliberately loose: a phrase reaching this point already contains a figure, and the
    /// worst a false match can do is produce "costing" in front of something that reads fine
    /// without it.
    /// </remarks>
    private static readonly string[] Qualifiers =
    [
        "about", "above", "approx", "approximately", "around", "at", "below", "between", "circa",
        "from", "less", "max", "maximum", "min", "minimum", "more", "near", "nearly", "no", "over",
        "roughly", "under", "up", "upto"
    ];

    /// <summary>
    /// The price as a finished sentence, punctuated, ready to stand as its own line in an email.
    /// Empty when the organizer set no price, which is the caller's cue to leave the line out.
    /// </summary>
    internal static string Describe(string priceRange)
    {
        var text = priceRange.Trim();

        if (text.Length == 0)
            return string.Empty;

        // Sorting ignores a full stop the organizer may have typed, so that "$25 - $40." is still
        // recognised as an amount. The other branch works from the original, so their punctuation
        // survives wherever it is their sentence being kept.
        var amount = text.TrimEnd('.', '!', '?', ' ');

        return NamesAnAmount(amount)
            ? $"Please purchase a gift costing {WithAnyLeadingQualifierInLowerCase(amount)}."
            : AsASentence(text);
    }

    /// <summary>
    /// Whether the text reads as an amount rather than as a remark about one.
    /// </summary>
    /// <remarks>
    /// Two things have to hold. There must be a figure somewhere, which is what separates "under
    /// $25" from "no limit" and "whatever you like". And the phrase has to open in a way that can
    /// follow "costing": a figure, a currency, or one of the <see cref="Qualifiers"/>. Anything
    /// else opening the phrase — "Keep it under $20", "Anything up to 30 dollars" — is the
    /// organizer's own sentence, however many figures it goes on to contain.
    /// </remarks>
    private static bool NamesAnAmount(string text)
    {
        if (!text.Any(char.IsDigit))
            return false;

        var opening = FirstWord(text);

        return opening.Length > 0
               && (OpensWithAFigure(opening)
                   || IsACurrencyCode(opening)
                   || Qualifiers.Contains(opening, StringComparer.OrdinalIgnoreCase));
    }

    private static bool OpensWithAFigure(string word) =>
        char.IsDigit(word[0]) || CharUnicodeInfo.GetUnicodeCategory(word[0]) == UnicodeCategory.CurrencySymbol;

    /// <summary>
    /// "USD 25" and "GBP 20" name an amount as surely as "$25" does, and the three-letter shape is
    /// specific enough to spot without keeping a list of currencies.
    /// </summary>
    private static bool IsACurrencyCode(string word) =>
        word.Length == 3 && word.All(char.IsAsciiLetterUpper);

    /// <summary>
    /// Lower-cases a leading qualifier so that "Around $100", typed by an organizer who was writing
    /// a line rather than filling a box, does not land capitalised in the middle of the sentence.
    /// Only words matched against <see cref="Qualifiers"/> are touched: a currency code has to keep
    /// its case, and a figure has none.
    /// </summary>
    private static string WithAnyLeadingQualifierInLowerCase(string text)
    {
        var opening = FirstWord(text);

        return Qualifiers.Contains(opening, StringComparer.OrdinalIgnoreCase)
            ? string.Concat(opening.ToLowerInvariant(), text.AsSpan(opening.Length))
            : text;
    }

    /// <summary>
    /// The organizer's own words, capitalised and closed off, since they are about to be read as a
    /// paragraph of their own rather than as part of a sentence this code wrote.
    /// </summary>
    private static string AsASentence(string text)
    {
        var opened = char.ToUpperInvariant(text[0]) + text[1..];

        return opened[^1] is '.' or '!' or '?' ? opened : $"{opened}.";
    }

    private static string FirstWord(string text)
    {
        var space = text.IndexOf(' ');
        return space < 0 ? text : text[..space];
    }
}
