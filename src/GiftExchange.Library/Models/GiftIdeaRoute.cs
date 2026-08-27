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
    /// The name of the person the sender drew.
    ///
    /// Not needed to deliver anything — it is here to be looked for. If it appears in the submitted
    /// text, the sender has quoted their own invitation into the message, and forwarding that would
    /// tell <see cref="Giver"/> who the sender drew. That is the one secret this application keeps.
    /// </summary>
    public required string SenderPickedRecipientName { get; init; }

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
        SenderPickedRecipientName = string.Empty,
        Giver = Persons.Empty
    };
}
