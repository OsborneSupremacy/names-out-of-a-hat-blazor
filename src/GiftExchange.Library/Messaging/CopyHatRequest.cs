namespace GiftExchange.Library.Messaging;

/// <summary>
/// A request to start a new gift exchange from a finished one: the same people, the same
/// eligibility rules, nobody assigned a recipient yet.
/// </summary>
internal record CopyHatRequest : IOrganizerScopedRequest
{
    /// <summary>The finished gift exchange being copied.</summary>
    public required Guid HatId { get; init; }

    public required string OrganizerEmail { get; init; }

    /// <summary>
    /// The name for the copy. A name is always asked for rather than derived, because the
    /// organizer cannot have two exchanges by the same name and only they know what this one is.
    /// </summary>
    public required string NewHatName { get; init; }

    /// <summary>
    /// When true, whoever a participant drew in the source exchange is not an eligible recipient
    /// for them in the copy — the reason most groups keep a record of last year's picks at all.
    /// </summary>
    public required bool ExcludePreviousRecipients { get; init; }

    IOrganizerScopedRequest IOrganizerScopedRequest.WithOrganizerEmail(string organizerEmail) =>
        this with { OrganizerEmail = organizerEmail };
}
