namespace GiftExchange.Library.Models;

/// <summary>
/// Everything an incoming gift ideas email needs resolved before it can be acted on, found from the
/// hash of the token in the address it was sent to.
///
/// One lookup rather than several because the handler needs all of it or none of it: who is allowed
/// to have sent this, whether the exchange is still taking submissions, and where the text goes.
/// </summary>
public record GiftIdeaRoute
{
    public required Guid ParticipantId { get; init; }

    public required Guid HatId { get; init; }

    public required string HatName { get; init; }

    /// <summary>One of <see cref="HatStatuses.All"/>. Submissions are only accepted for some of them.</summary>
    public required string HatStatus { get; init; }

    /// <summary>
    /// The participant sharing the ideas. Their address is what an incoming message has to have
    /// come from: the token says which row to write, and this says who is allowed to write it.
    /// </summary>
    public required Person Sender { get; init; }

    /// <summary>
    /// The person the sender drew.
    ///
    /// Serves two unrelated purposes. Their name is here to be looked for: if it appears in
    /// submitted text, the sender has quoted their own invitation into the message, and forwarding
    /// that would tell <see cref="Giver"/> who the sender drew — the one secret this application
    /// keeps. Their address is where an Ask goes, since the sender asking for gift ideas is asking
    /// this person for them.
    /// </summary>
    public required Person SenderPickedRecipient { get; init; }

    /// <summary>
    /// The participant row behind <see cref="SenderPickedRecipient"/>, or the all-zero id if the
    /// hat has not been shaken. An Ask needs it to issue them a token of their own.
    /// </summary>
    public required Guid SenderPickedRecipientParticipantId { get; init; }

    /// <summary>
    /// Whoever drew the sender, and so the single person these ideas are for. Telling them that the
    /// sender wrote something reveals nothing: they already know whose name they hold. The reverse
    /// is never sent — the sender is not told who this is.
    /// </summary>
    public required Person Giver { get; init; }
}

internal static class GiftIdeaRoutes
{
    public static GiftIdeaRoute Empty => new()
    {
        ParticipantId = Guid.Empty,
        HatId = Guid.Empty,
        HatName = string.Empty,
        HatStatus = string.Empty,
        Sender = Persons.Empty,
        SenderPickedRecipient = Persons.Empty,
        SenderPickedRecipientParticipantId = Guid.Empty,
        Giver = Persons.Empty
    };
}
