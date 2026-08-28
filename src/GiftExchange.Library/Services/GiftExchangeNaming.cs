namespace GiftExchange.Library.Services;

/// <summary>
/// How an exchange is referred to in anything a participant reads.
/// </summary>
/// <remarks>
/// Organizers name exchanges whatever they like, and a name is rarely a noun phrase that can be
/// dropped straight into a sentence. Slotting one in behind an article produced "added you to the
/// Christmas On August 27!", and no amount of adjusting the words around it fixes that generally:
/// the next name will want a different article, or none, or a preposition.
///
/// So the sentence never leans on the name. "the gift exchange" carries it, and the organizer's
/// name for the exchange follows as an aside, which reads the same whatever they called it. The
/// aside is a parenthetical: a caller placing this mid-sentence has to close it with a comma.
/// </remarks>
internal static class GiftExchangeNaming
{
    /// <summary>
    /// The exchange as it should be named in a sentence, with no trailing punctuation.
    /// </summary>
    internal static string Describe(string name) =>
        string.IsNullOrWhiteSpace(name) ? "the gift exchange" : $"the gift exchange, {name.Trim()}";

    /// <summary>
    /// The exchange named part-way through a sentence, with the aside closed off so that the
    /// sentence can carry on past it.
    /// </summary>
    /// <remarks>
    /// The closing comma belongs to the aside rather than to the sentence, so it appears only when
    /// there is an aside — "the gift exchange, Family Christmas, is over" against "the gift
    /// exchange is over" for an exchange nobody named. Callers that finish on the name want
    /// <see cref="Describe"/> instead, which leaves the punctuation to them.
    /// </remarks>
    internal static string DescribeMidSentence(string name) =>
        string.IsNullOrWhiteSpace(name) ? Describe(name) : $"{Describe(name)},";

    /// <summary>
    /// The same again, capitalised to open a sentence or a subject line.
    /// </summary>
    /// <remarks>
    /// Only the leading article is capitalised. The organizer's own name for the exchange is left
    /// exactly as they typed it, which matters for the ones that start lower case on purpose.
    /// </remarks>
    internal static string DescribeToOpenASentence(string name)
    {
        var described = DescribeMidSentence(name);
        return char.ToUpperInvariant(described[0]) + described[1..];
    }
}
