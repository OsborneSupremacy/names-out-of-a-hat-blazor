namespace GiftExchange.Library.Messaging;

/// <summary>
/// What the correction did, which is not always the same thing.
/// </summary>
/// <remarks>
/// Before invitations go out an address change is only a change of address, and nothing is sent.
/// Afterwards it necessarily sends, because the point of fixing the address is that somebody never
/// received what was sent to the old one. The organizer is told which happened rather than left to
/// infer it from the hat's status — mail going out on their behalf should never be a surprise.
/// </remarks>
public record EditParticipantAddressResponse
{
    /// <summary>Whether an email was queued to the new address.</summary>
    public required bool EmailResent { get; init; }

    /// <summary>
    /// Which email, when one was sent: one of <see cref="EmailMessageType"/>, or empty when none
    /// was. An exchange that has already been revealed resends the announcement rather than the
    /// invitation, because the invitation is no longer true.
    /// </summary>
    public required string MessageType { get; init; }
}
