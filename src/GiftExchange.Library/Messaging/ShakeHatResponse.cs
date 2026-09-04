namespace GiftExchange.Library.Messaging;

/// <summary>
/// The outcome of a shake. <see cref="Participants"/> is empty unless <see cref="Success"/>, so a
/// caller that ignores the flag assigns nobody rather than assigning half the hat.
/// </summary>
internal record ShakeHatResponse
{
    public required bool Success { get; init; }

    /// <summary>
    /// Every participant, each with <see cref="Models.Participant.PickedRecipient"/> filled in.
    /// Empty on failure.
    /// </summary>
    public required ImmutableList<Participant> Participants { get; init; }

    internal static ShakeHatResponse Failed => new() { Success = false, Participants = [] };
}
