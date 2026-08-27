namespace GiftExchange.Library.Abstractions;

/// <summary>
/// Caps how often this application will answer an unsolicited message from any one address.
/// </summary>
public interface IReplyThrottleProvider
{
    /// <summary>Claims this address's one reply for the window.</summary>
    /// <returns>false when it has already been answered inside the window.</returns>
    Task<bool> TryReserveReplySlotAsync(string kind, string email);
}
