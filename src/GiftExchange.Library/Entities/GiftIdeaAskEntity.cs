namespace GiftExchange.Library.Entities;

/// <summary>
/// One participant asking another for gift ideas about a third.
///
/// The original Ask needed no row of its own: it went to the person the asker drew, for ideas about
/// themselves, and <see cref="GiftIdeaTokenEntity"/> already routes that. Asking anybody else pulls
/// those roles apart — the asker, the helper and the subject are three different participants — and
/// a token that names one of them cannot say which. So the ask itself is written down, and the
/// token issued for it points here.
/// </summary>
public class GiftIdeaAskEntity
{
    public required Guid GiftIdeaAskId { get; set; }

    /// <summary>
    /// Who asked, and so where anything sent in reply goes. Never named to the helper: not being
    /// named is the whole promise the button makes.
    /// </summary>
    /// <remarks>
    /// No navigation property, for the reason given on <see cref="GiftIdeaEntity.ParticipantId"/>.
    /// The same applies to the two ids below.
    /// </remarks>
    public required Guid AskerParticipantId { get; set; }

    /// <summary>
    /// Who was asked. The only address a submission on this ask's token may come from, which is
    /// what stops a forwarded address from becoming somebody else's way in.
    /// </summary>
    public required Guid HelperParticipantId { get; set; }

    /// <summary>
    /// Who the ideas are about — the asker's pick as it stood when they asked.
    ///
    /// Recorded rather than followed back through <see cref="ParticipantEntity.PickedRecipientParticipantId"/>
    /// at forwarding time. The helper has already been told a name, and an organizer editing picks
    /// afterwards must not quietly re-point an ask that is sitting in somebody's inbox.
    /// </summary>
    public required Guid SubjectParticipantId { get; set; }

    /// <summary>
    /// Hex-encoded SHA-256 of the token, as <see cref="GiftIdeaTokenEntity.TokenHash"/> holds one
    /// and for the same reason.
    /// </summary>
    public required string TokenHash { get; set; }

    public required DateTimeOffset IssuedAt { get; set; }
}
