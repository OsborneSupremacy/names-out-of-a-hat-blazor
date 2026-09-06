namespace GiftExchange.Library.Messaging;

/// <summary>
/// What <c>GiftExchangeProvider.RenamePersonAsync</c> needs to change the name somebody goes by.
/// </summary>
/// <remarks>
/// Person-scoped rather than hat-scoped, because that is what a name is. Both endpoints that change
/// one — an organizer editing a participant, and somebody editing their own profile — arrive here,
/// and the difference between them is only which address is in <see cref="Email"/>.
/// </remarks>
internal record RenamePersonRequest
{
    /// <summary>The address of the person to rename, which is how they are found.</summary>
    public required string Email { get; init; }

    public required string Name { get; init; }

    /// <summary>
    /// The caller. Decides two things: whether the rename is theirs to make at all, and which
    /// colliding exchanges may be named back to them.
    /// </summary>
    public required string RequestedByEmail { get; init; }
}
