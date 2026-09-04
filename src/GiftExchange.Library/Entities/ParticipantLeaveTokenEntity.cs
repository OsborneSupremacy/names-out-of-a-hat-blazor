namespace GiftExchange.Library.Entities;

/// <summary>
/// The token behind the leave link in a participant's invitation, held as a hash.
/// </summary>
/// <remarks>
/// Its own table rather than a second use of <see cref="GiftIdeaTokenEntity"/>, because the two
/// authorise different things and one token doing both would be the wrong trade in both directions.
/// A gift ideas token is handed to other participants by the Ask so somebody can be asked what they
/// would like; that is a reasonable thing to widen, and a poor thing to attach a removal to.
///
/// Issued when invitations go out, and never for the organizer — there is no leaving an exchange
/// you are running, and the clearest way to say so is for no token of theirs to exist to be found.
/// </remarks>
public class ParticipantLeaveTokenEntity
{
    public required Guid ParticipantLeaveTokenId { get; set; }

    /// <summary>
    /// The participant this token removes. No navigation property, for the reason given on
    /// <see cref="GiftIdeaEntity.ParticipantId"/>.
    /// </summary>
    public required Guid ParticipantId { get; set; }

    /// <summary>
    /// Hex-encoded SHA-256 of the token, as <see cref="GiftIdeaTokenEntity.TokenHash"/> holds one:
    /// a link is matched by hashing what arrived and looking for it, so a dump of this table lets
    /// nobody leave anything.
    /// </summary>
    public required string TokenHash { get; set; }

    public required DateTimeOffset IssuedAt { get; set; }
}
