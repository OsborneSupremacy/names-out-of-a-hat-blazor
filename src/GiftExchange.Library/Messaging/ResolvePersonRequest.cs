namespace GiftExchange.Library.Messaging;

/// <summary>
/// What <c>GiftExchangeProvider.ResolvePersonIdAsync</c> needs to find somebody, or write them in
/// if the application has never seen them.
/// </summary>
internal record ResolvePersonRequest
{
    public required string Email { get; init; }

    /// <summary>
    /// What <see cref="IntroducedByEmail"/> says this person is called.
    /// </summary>
    /// <remarks>
    /// Applied to somebody the application already knows only when the caller has standing to say
    /// what they are called — see <c>PersonEntity.AddedByPersonId</c>. Otherwise the name already
    /// on the row stands, and this is the name that was not taken.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Who is introducing them. The same address as <see cref="Email"/> when somebody is arriving
    /// under their own steam, which is what an organizer creating their first exchange is doing.
    /// </summary>
    public required string IntroducedByEmail { get; init; }
}
