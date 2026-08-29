namespace GiftExchange.Library.Abstractions;

/// <summary>
/// Caps how often this application will act on a repeated request from the same source.
/// </summary>
/// <remarks>
/// Internal because the two slot methods speak in <c>Messaging</c> records, which are internal. The
/// test project reaches it through <c>InternalsVisibleTo</c>, and NSubstitute through
/// <c>DynamicProxyGenAssembly2</c>.
/// </remarks>
internal interface IReplyThrottleProvider
{
    /// <summary>Claims this address's one reply for the window.</summary>
    /// <returns>false when it has already been answered inside the window.</returns>
    Task<bool> TryReserveReplySlotAsync(string kind, string email);

    /// <summary>
    /// Claims one participant's Ask to another for the window.
    /// </summary>
    /// <remarks>See <see cref="ReserveAskSlotRequest"/> for why the slot is held per pair.</remarks>
    Task<ReserveSlotResponse> TryReserveAskSlotAsync(ReserveAskSlotRequest request);

    /// <summary>
    /// Claims one participant's address correction for the window.
    /// </summary>
    /// <remarks>
    /// See <see cref="ReserveAddressChangeSlotRequest"/> for why the slot is held per participant,
    /// and for what this limit is and is not.
    /// </remarks>
    Task<ReserveSlotResponse> TryReserveAddressChangeSlotAsync(ReserveAddressChangeSlotRequest request);
}
