namespace GiftExchange.Library.Services;

/// <summary>
/// Some hours after invitations go out, tells the organizer which of them came back.
/// </summary>
/// <remarks>
/// The delivery column made bounces visible; this makes them noticed. An organizer's last act is
/// pressing send, and nothing afterwards brings them back to the page -- so a wrong address sat in
/// the table until somebody at the exchange mentioned they never got a name, which is usually far
/// too late to do anything about.
///
/// Delayed rather than immediate, and that delay is the whole design. SES publishes a bounce when
/// it gives up, not when it first fails, and a transient problem at the far end can be retried for
/// a long time before either outcome is known. A notice sent minutes after a send would list
/// addresses that were about to work and miss the ones that were not, which is worse than no notice
/// at all: it would teach organizers that this email is noise.
///
/// It reads the same view of delivery the organizer's own page reads, deliberately. An email that
/// derived its own answer from the same table would eventually disagree with the screen the
/// organizer is being sent to look at, and the screen is where the fix is.
/// </remarks>
[UsedImplicitly]
internal class UndeliverableInvitationsService
{
    /// <summary>
    /// Statuses in which there is no longer anything to be done about a bad address.
    /// </summary>
    /// <remarks>
    /// Only <c>CLOSED</c>. Everything before it is still worth writing about, including the
    /// statuses an exchange falls back to when somebody leaves: an organizer about to shake the hat
    /// and send again is precisely the person who most wants to know which addresses failed last
    /// time, since fixing them now costs nothing and fixing them after the next send costs another
    /// round of confusion.
    /// </remarks>
    private static readonly ImmutableList<string> SilentStatuses = [HatStatus.Closed];

    private readonly GiftExchangeProvider _giftExchangeProvider;

    private readonly UndeliverableInvitationsEmailCompositionService _emailCompositionService;

    private readonly AutomaticEmailSender _emailSender;

    private readonly ILogger<UndeliverableInvitationsService> _logger;

    public UndeliverableInvitationsService(
        GiftExchangeProvider giftExchangeProvider,
        UndeliverableInvitationsEmailCompositionService emailCompositionService,
        AutomaticEmailSender emailSender,
        ILogger<UndeliverableInvitationsService> logger
    )
    {
        _giftExchangeProvider = giftExchangeProvider ?? throw new ArgumentNullException(nameof(giftExchangeProvider));
        _emailCompositionService = emailCompositionService ?? throw new ArgumentNullException(nameof(emailCompositionService));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <returns>
    /// Whether a notice was sent. False is the ordinary outcome and not a failure -- most exchanges
    /// have nothing wrong with them.
    /// </returns>
    internal async Task<bool> ExecuteAsync(UndeliverableInvitationsScheduleRequest request)
    {
        if (request.HatId == Guid.Empty || string.IsNullOrWhiteSpace(request.OrganizerEmail))
        {
            _logger.LogError(
                "Cannot check invitation delivery: the schedule payload named no hat or no organizer. HatId: {HatId}; OrganizerEmail present: {HasOrganizerEmail}",
                request.HatId,
                !string.IsNullOrWhiteSpace(request.OrganizerEmail));
            return false;
        }

        // Scoped by the organizer, so a schedule can only ever read the hat it was created for. A
        // hat deleted in the meantime resolves to nothing here, which is the right outcome: the
        // schedule outlives the exchange it was made for and has to cope with finding it gone.
        var (exists, hat) = await _giftExchangeProvider
            .GetHatAsync(request.OrganizerEmail, request.HatId)
            .ConfigureAwait(false);

        if (!exists)
        {
            _logger.LogInformation(
                "No invitation delivery notice for hat {HatId}: it no longer exists, or is not {OrganizerEmail}'s.",
                request.HatId,
                request.OrganizerEmail);
            return false;
        }

        if (SilentStatuses.Contains(hat.Status))
        {
            _logger.LogInformation(
                "No invitation delivery notice for hat {HatId}: it is {HatStatus}.",
                request.HatId,
                hat.Status);
            return false;
        }

        var undeliverable = Undeliverable(hat);

        if (undeliverable.Count == 0)
        {
            _logger.LogInformation(
                "No invitation delivery notice for hat {HatId}: every invitation we have heard about is fine.",
                request.HatId);
            return false;
        }

        var notice = new ComposeUndeliverableNoticeRequest { Hat = hat, Undeliverable = undeliverable };

        await _emailSender
            .SendAsync(
                hat.Organizer.Email,
                UndeliverableInvitationsEmailCompositionService.GetSubject(notice),
                _emailCompositionService.ComposeEmail(notice))
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Told the organizer of hat {HatId} about {Count} undeliverable invitations.",
            request.HatId,
            undeliverable.Count);

        return true;
    }

    /// <summary>
    /// The participants whose invitation is known not to have arrived.
    /// </summary>
    /// <remarks>
    /// Two conditions, and both of them are about not overstating what is known.
    ///
    /// The status has to be one of <see cref="DeliveryStatuses.Undeliverable"/>. A participant
    /// nothing has been heard about is not a participant who missed their invitation -- SES reports
    /// asynchronously and events do go missing -- and listing them would send an organizer to
    /// pester somebody holding theirs.
    ///
    /// The message has to be the invitation. What the provider hands back is the newest event of
    /// any type, so a bounced completion email against a closed exchange would otherwise be
    /// reported as a failed invitation weeks after the fact. This is also why an invitation that
    /// bounced and was followed by some other message to the same person drops off the list: the
    /// newest row is what the organizer's own page shows, and the two must not disagree.
    ///
    /// There is deliberately no check that the failure happened after this send. The row kept per
    /// participant is the newest one, so a corrected address that now delivers replaces the bounce
    /// by itself, and a re-send produces its own events well inside the hours this waits.
    /// </remarks>
    private static ImmutableList<Participant> Undeliverable(Hat hat) =>
        hat.Participants
            .Where(participant => participant.DeliveryMessageType == EmailMessageType.Invitation)
            .Where(participant => DeliveryStatuses.IsUndeliverable(participant.DeliveryStatus))
            .OrderBy(participant => participant.Person.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableList();
}
