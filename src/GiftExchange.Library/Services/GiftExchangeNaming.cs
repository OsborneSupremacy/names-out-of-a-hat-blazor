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
}
