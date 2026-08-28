namespace GiftExchange.Library.Messaging;

internal record GiftExchangeEmailRequest
{
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    public required string RecipientEmail { get; init; }

    /// <summary>
    /// Which participant this is going to, so that the send can be tagged with it and the SES
    /// events that follow can be matched back to a row.
    /// </summary>
    /// <remarks>
    /// The address will not do that job. In test mode every message is diverted to one inbox, and
    /// even in live use a single address can be in several exchanges at once — so what comes back
    /// on an event has to name the participant, not the person.
    /// </remarks>
    public required Guid ParticipantId { get; init; }

    /// <summary>One of <see cref="EmailMessageType"/>. Tagged onto the send for the same reason.</summary>
    public required string MessageType { get; init; }

    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }
}
