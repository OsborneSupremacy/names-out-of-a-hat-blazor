namespace GiftExchange.Library.Models;

/// <summary>
/// What shape the organizer wants the draw to come out in — the rule the shake has to satisfy
/// beyond the one that never varies, which is that everybody gives to exactly one person and
/// receives from exactly one person.
///
/// Strings rather than an enum, for the reason <see cref="FeedbackCategory"/> is: the value crosses
/// the wire, and an enum would serialize as an integer under the source-generated serializer, so
/// the schema API Gateway validates against would describe a number nobody can read.
/// </summary>
/// <remarks>
/// The vocabulary is deliberately two-layered. These names, and the ones
/// <see cref="DrawTypes.Describe"/> returns, are what an organizer sees; the remarks below are the
/// permutation language the code is actually written in, and the reason each rule is expressible at
/// all. A draw is a permutation of the participants with no fixed point — a derangement — and every
/// permutation decomposes into disjoint cycles. All three of these are statements about cycles.
/// </remarks>
public static class DrawType
{
    /// <summary>
    /// No rule beyond the exclusions the organizer already set. Any cycle structure is accepted,
    /// including two people who drew each other.
    /// </summary>
    public static string AnythingGoes => "ANYTHING_GOES";

    /// <summary>
    /// No two people may draw each other — no 2-cycle. Every cycle in the result is three people
    /// or longer.
    /// </summary>
    public static string NoMutualPairs => "NO_MUTUAL_PAIRS";

    /// <summary>
    /// Everybody in one unbroken chain: a single cycle through all of them, a Hamiltonian cycle in
    /// the graph of who may draw whom. The strictest of the three, and it implies
    /// <see cref="NoMutualPairs"/> for any exchange of more than two people.
    /// </summary>
    public static string SingleCycle => "SINGLE_CYCLE";
}

public static class DrawTypes
{
    /// <summary>
    /// The draw types the shake dialog offers, and the only ones the endpoint accepts. Adding one
    /// here is not enough on its own — the JSON schema enum, the shaker's own dispatch and the
    /// frontend's list are the other three places, and <c>SchemaDriftTests</c> keeps this list and
    /// the schema together.
    /// </summary>
    public static readonly ImmutableList<string> All =
    [
        DrawType.AnythingGoes,
        DrawType.NoMutualPairs,
        DrawType.SingleCycle
    ];

    /// <summary>
    /// How a draw type reads in a sentence written for an organizer. Kept beside the draw types
    /// rather than in the service, so that adding one and forgetting to label it fails to compile
    /// rather than arriving in an error message spelled NO_MUTUAL_PAIRS.
    /// </summary>
    public static string Describe(string drawType) => drawType switch
    {
        var value when value == DrawType.AnythingGoes => "Anything goes",
        var value when value == DrawType.NoMutualPairs => "No mutual pairs",
        var value when value == DrawType.SingleCycle => "Single cycle",
        _ => drawType
    };

    /// <summary>
    /// Whether this draw type asks for anything beyond the organizer's exclusions. The two that do
    /// are the two that can turn a satisfiable set of exclusions into an unsatisfiable one, so this
    /// is what decides how hard the shaker tries and what it says when it gives up.
    /// </summary>
    public static bool IsConstrained(string drawType) => drawType != DrawType.AnythingGoes;
}
