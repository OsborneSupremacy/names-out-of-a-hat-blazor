namespace GiftExchange.Library.Messaging;

public record CopyHatResponse
{
    /// <summary>The new gift exchange. The source is left untouched.</summary>
    public required Guid HatId { get; init; }

    /// <summary>
    /// How many of the source exchange's participants were not carried over because they had asked
    /// not to be added.
    /// </summary>
    /// <remarks>
    /// A count and not a list of names, deliberately. The organizer needs to know that the copy is
    /// smaller than what it was copied from, or they will send invitations to a short exchange
    /// without noticing; they do not need to be handed the identity of everybody who opted out,
    /// which is the fact those people withheld. Naming them here would undo the refusal by another
    /// route, since an organizer holding both lists can subtract one from the other.
    ///
    /// Zero on the ordinary copy, which is nearly all of them.
    /// </remarks>
    public required int ParticipantsOmitted { get; init; }
}
