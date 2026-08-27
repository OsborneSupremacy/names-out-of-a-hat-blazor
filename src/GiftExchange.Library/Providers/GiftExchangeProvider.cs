using System.Data;
using GiftExchange.Library.Contexts;
using Npgsql;

namespace GiftExchange.Library.Providers;

/// <summary>
/// Data access for gift exchanges.
///
/// The public surface still speaks the domain records, which identify people by display name and
/// email address. Storage no longer does: a person is a row of their own, and hats and participants
/// point at it, so this class translates at the boundary. That containment is deliberate — the API
/// contract still carries names, so changing it is a separate job.
///
/// A session arrives carrying an email address, so almost every method here starts by turning one
/// into a person id. Reads do that with a join through the <c>Organizer</c> navigation; writes look
/// the id up first and then filter on it directly, which keeps the SQL behind ExecuteUpdate and
/// ExecuteDelete free of subqueries that DSQL may or may not accept.
/// </summary>
[UsedImplicitly]
public class GiftExchangeProvider
{
    /// <summary>Postgres unique_violation.</summary>
    private const string UniqueViolation = "23505";

    /// <summary>One gift exchange name per organizer.</summary>
    private const string HatNamePerOrganizerIndex = "uq_hat_organizer_name";

    /// <summary>One person per email address, for the whole application.</summary>
    private const string PersonEmailIndex = "uq_person_email";

    private readonly IDbContextFactory<GiftExchangeDbContext> _contextFactory;

    private readonly ILogger<GiftExchangeProvider> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GiftExchangeProvider(
        IDbContextFactory<GiftExchangeDbContext> contextFactory,
        ILogger<GiftExchangeProvider> logger
    )
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(string organizerName, ImmutableList<HatMetaData> hats)> GetHatsAsync(string organizerEmail)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // The sentinel person holds the empty address, so an empty one here would match it and go on
        // to return the sentinel hat as though it were theirs. Nothing upstream should send one --
        // the address comes from the authorizer -- which is exactly why it is cheap to refuse.
        if (string.IsNullOrWhiteSpace(organizerEmail))
            return (string.Empty, []);

        // The name comes from the person, not from the newest hat, so somebody who has signed in
        // but not created an exchange yet is still greeted by name.
        var organizer = await context.Persons
            .AsNoTracking()
            .Where(person => person.Email == organizerEmail)
            .Select(person => new { person.PersonId, person.Name })
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (organizer is null)
            return (string.Empty, []);

        var hats = await context.Hats
            .AsNoTracking()
            .Where(hat => hat.OrganizerPersonId == organizer.PersonId)
            .OrderByDescending(hat => hat.CreatedAt)
            .Select(hat => new HatMetaData
            {
                HatId = hat.HatId,
                HatName = hat.Name,
                Status = hat.Status
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return (organizer.Name, hats.ToImmutableList());
    }

    /// <returns>true if the hat was created, false if the organizer already has one by that name.</returns>
    public async Task<bool> CreateHatAsync(HatDataModel hatDataModel)
    {
        var organizerPersonId = await ResolvePersonIdAsync(hatDataModel.OrganizerEmail, hatDataModel.OrganizerName)
            .ConfigureAwait(false);

        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        context.Hats.Add(new HatEntity
        {
            HatId = hatDataModel.HatId,
            OrganizerPersonId = organizerPersonId,
            Name = hatDataModel.HatName,
            NameNormalized = Normalize(hatDataModel.HatName),
            Status = hatDataModel.Status,
            AdditionalInformation = hatDataModel.AdditionalInformation,
            PriceRange = hatDataModel.PriceRange,
            InvitationsQueuedAt = DateTimeOffset.MinValue,
            InvitationsSentFromIp = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            CopiedFromHatId = Guid.Empty
        });

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, HatNamePerOrganizerIndex))
        {
            // The caller checks for a duplicate name first; this closes the window between that
            // check and this write.
            return false;
        }
    }

    /// <summary>
    /// Writes a new hat holding the same people and the same eligibility rules as an existing
    /// one, with nobody assigned a recipient. Everything lands in one transaction, so a failure
    /// part way through cannot leave a hat with half its participants.
    ///
    /// Participant rows are re-created with new ids, but they point at the same people: a copy is a
    /// second exchange among the same group, not a second set of humans. The eligibility rows are
    /// translated through the mapping from old participant id to new. Matching on name would have
    /// been simpler and wrong: two people in a hat may share a display name.
    ///
    /// The copy records the hat it came from. Nothing reads that yet — it is what a future rule
    /// along the lines of "nobody draws the same person two years running" would need, and it can
    /// only be captured at the moment the copy is made.
    /// </summary>
    /// <param name="sourceHatId">The hat being copied. It is only read.</param>
    /// <param name="newHat">The hat to write. Its organizer scopes the read of the source.</param>
    /// <param name="excludePreviousRecipients">
    /// When true, the participant somebody drew in the source hat is left out of their
    /// eligibility list in the copy.
    /// </param>
    /// <returns>true if the copy was written, false if the organizer already has a hat by that name.</returns>
    public async Task<bool> CopyHatAsync(
        Guid sourceHatId,
        HatDataModel newHat,
        bool excludePreviousRecipients
    )
    {
        try
        {
            // Resolved before the transaction opens: the organizer already exists, since they own
            // the hat being copied, but this is where a person is written and it does not belong
            // inside the unit of work that writes the copy.
            var organizerPersonId = await ResolvePersonIdAsync(newHat.OrganizerEmail, newHat.OrganizerName)
                .ConfigureAwait(false);

            await InTransactionAsync(async context =>
            {
                var source = await context.Hats
                    .AsNoTracking()
                    .Include(hat => hat.Participants).ThenInclude(participant => participant.EligibleRecipients)
                    .SingleAsync(hat => hat.HatId == sourceHatId && hat.OrganizerPersonId == organizerPersonId)
                    .ConfigureAwait(false);

                context.Hats.Add(new HatEntity
                {
                    HatId = newHat.HatId,
                    OrganizerPersonId = organizerPersonId,
                    Name = newHat.HatName,
                    NameNormalized = Normalize(newHat.HatName),
                    Status = newHat.Status,
                    AdditionalInformation = newHat.AdditionalInformation,
                    PriceRange = newHat.PriceRange,
                    InvitationsQueuedAt = DateTimeOffset.MinValue,
                    InvitationsSentFromIp = string.Empty,
                    CreatedAt = DateTimeOffset.UtcNow,
                    // Copying a copy records the hat it came from, not the one at the head of the
                    // chain, so the chain stays walkable one link at a time.
                    CopiedFromHatId = sourceHatId
                });

                var newParticipantIds = source.Participants
                    .ToDictionary(participant => participant.ParticipantId, _ => Guid.CreateVersion7());

                foreach (var participant in source.Participants)
                    context.Participants.Add(new ParticipantEntity
                    {
                        ParticipantId = newParticipantIds[participant.ParticipantId],
                        HatId = newHat.HatId,
                        PersonId = participant.PersonId,
                        // A copy has not been shaken, which is the whole point of making one.
                        PickedRecipientParticipantId = Guid.Empty
                    });

                foreach (var participant in source.Participants)
                foreach (var eligibility in participant.EligibleRecipients)
                {
                    if (excludePreviousRecipients
                        && eligibility.EligibleParticipantId == participant.PickedRecipientParticipantId)
                        continue;

                    // Nothing enforces referential integrity in DSQL, so a row pointing at a
                    // participant who is no longer in the hat is copied as nothing at all rather
                    // than taken on faith.
                    if (!newParticipantIds.TryGetValue(eligibility.EligibleParticipantId, out var eligibleId))
                    {
                        _logger.LogWarning(
                            "Skipped an eligibility row while copying hat {SourceHatId}: recipient {EligibleParticipantId} is not a participant in it.",
                            sourceHatId,
                            eligibility.EligibleParticipantId);
                        continue;
                    }

                    context.ParticipantEligibleRecipients.Add(new ParticipantEligibleRecipientEntity
                    {
                        ParticipantEligibleRecipientId = Guid.CreateVersion7(),
                        ParticipantId = newParticipantIds[participant.ParticipantId],
                        EligibleParticipantId = eligibleId
                    });
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);

            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, HatNamePerOrganizerIndex))
        {
            // The caller checks the name first; this closes the window between that check and
            // this write, as it does for a hat created from scratch.
            return false;
        }
    }

    public async Task<(bool exists, Guid hatId)> DoesHatAlreadyExistAsync(string organizerEmail, string hatName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var organizerPersonId = await FindPersonIdByEmailAsync(context, organizerEmail).ConfigureAwait(false);

        if (organizerPersonId == Guid.Empty)
            return (false, Guid.Empty);

        var normalized = Normalize(hatName);

        var hatId = await context.Hats
            .AsNoTracking()
            .Where(hat => hat.OrganizerPersonId == organizerPersonId && hat.NameNormalized == normalized)
            .Select(hat => hat.HatId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        return hatId == Guid.Empty ? (false, Guid.Empty) : (true, hatId);
    }

    public async Task<(bool exists, Hat hat)> GetHatAsync(string organizerEmail, Guid hatId)
    {
        if (string.IsNullOrWhiteSpace(organizerEmail) || hatId == Guid.Empty)
            return (false, Hats.Empty);

        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var hat = await context.Hats
            .AsNoTracking()
            .Include(entity => entity.Organizer)
            .Include(entity => entity.Participants).ThenInclude(participant => participant.Person)
            .Include(entity => entity.Participants).ThenInclude(participant => participant.EligibleRecipients)
            .ThenInclude(eligible => eligible.EligibleParticipant).ThenInclude(participant => participant.Person)
            .SingleOrDefaultAsync(entity => entity.HatId == hatId && entity.Organizer.Email == organizerEmail)
            .ConfigureAwait(false);

        if (hat is null)
            return (false, Hats.Empty);

        return (true, new Hat
        {
            Id = hat.HatId,
            Name = hat.Name,
            Status = hat.Status,
            AdditionalInformation = hat.AdditionalInformation,
            PriceRange = hat.PriceRange,
            Organizer = new Person { Name = hat.Organizer.Name, Email = hat.Organizer.Email },
            Participants = ToDomain(hat.Participants),
            InvitationsQueuedDate = hat.InvitationsQueuedAt
        });
    }

    public async Task EditHatAsync(EditHatRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var organizerPersonId = await FindPersonIdByEmailAsync(context, request.OrganizerEmail).ConfigureAwait(false);

        if (organizerPersonId == Guid.Empty)
            return;

        await context.Hats
            .Where(hat => hat.HatId == request.HatId && hat.OrganizerPersonId == organizerPersonId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(hat => hat.Name, request.Name)
                .SetProperty(hat => hat.NameNormalized, Normalize(request.Name))
                .SetProperty(hat => hat.AdditionalInformation, request.AdditionalInformation)
                .SetProperty(hat => hat.PriceRange, request.PriceRange))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the hat and everything hanging off it. DSQL has no ON DELETE CASCADE, so the
    /// order is explicit, and the whole thing runs in one transaction.
    ///
    /// People are left alone. They exist independently of any one exchange, and the participants
    /// being removed here may well be in somebody else's hat.
    ///
    /// Does nothing unless the hat belongs to the organizer asking.
    /// </summary>
    public Task DeleteHatAsync(DeleteHatRequest request) =>
        InTransactionAsync(async context =>
        {
            var organizerPersonId = await FindPersonIdByEmailAsync(context, request.OrganizerEmail).ConfigureAwait(false);

            if (organizerPersonId == Guid.Empty)
                return;

            // Ownership is established before anything is removed, not as part of removing the hat
            // last. Scoping only that final statement meant the two below ran against a hat id
            // nobody had checked: passing somebody else's emptied their exchange of participants
            // and eligibility while leaving the hat itself standing, and passing the all-zero id
            // reached the sentinel participant. Nothing upstream covers this — DeleteHatService
            // hands the request straight here.
            var ownsHat = await context.Hats
                .AnyAsync(hat => hat.HatId == request.HatId && hat.OrganizerPersonId == organizerPersonId)
                .ConfigureAwait(false);

            if (!ownsHat)
                return;

            var participantIds = context.Participants
                .Where(participant => participant.HatId == request.HatId)
                .Select(participant => participant.ParticipantId);

            await context.ParticipantEligibleRecipients
                .Where(row => participantIds.Contains(row.ParticipantId)
                              || participantIds.Contains(row.EligibleParticipantId))
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            await context.Participants
                .Where(participant => participant.HatId == request.HatId)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            // Any copy taken from this hat would otherwise point at an exchange that no longer
            // exists. Clearing it says "not a copy", which is true once the source is gone, and is
            // the same cleanup DeleteParticipantAsync does for a pick. Only this organizer's hats
            // can be affected: CopyHatAsync will not copy a hat you do not own.
            await context.Hats
                .Where(hat => hat.CopiedFromHatId == request.HatId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(hat => hat.CopiedFromHatId, Guid.Empty))
                .ConfigureAwait(false);

            await context.Hats
                .Where(hat => hat.HatId == request.HatId && hat.OrganizerPersonId == organizerPersonId)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);
        });

    public async Task UpdateHatStatusAsync(string organizerEmail, Guid hatId, string newStatus)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var organizerPersonId = await FindPersonIdByEmailAsync(context, organizerEmail).ConfigureAwait(false);

        if (organizerPersonId == Guid.Empty)
            return;

        await context.Hats
            .Where(hat => hat.HatId == hatId && hat.OrganizerPersonId == organizerPersonId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(hat => hat.Status, newStatus))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Hats belonging to this organizer where somebody other than them already goes by the given
    /// name. Renaming into one of those would leave two participants sharing a name.
    /// </summary>
    public async Task<ImmutableList<string>> FindHatsWhereParticipantNameIsTakenAsync(
        string organizerEmail,
        string name
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var organizerPersonId = await FindPersonIdByEmailAsync(context, organizerEmail).ConfigureAwait(false);

        if (organizerPersonId == Guid.Empty)
            return [];

        var normalized = Normalize(name);

        var hatNames = await context.Hats
            .AsNoTracking()
            // ToLower, not ToLowerInvariant: see FindParticipantIdByNameAsync.
            .Where(hat => hat.OrganizerPersonId == organizerPersonId
                          && hat.Participants.Any(participant => participant.PersonId != organizerPersonId
                                                                 && participant.Person.Name.ToLower() == normalized))
            .Select(hat => hat.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        return hatNames.ToImmutableList();
    }

    /// <summary>
    /// Changes the name this person is known by.
    /// </summary>
    /// <remarks>
    /// A name is stored once, on the person, so this is a single statement where it used to be a
    /// transaction spanning every hat they organize and every participant row within them.
    ///
    /// The other side of that is reach: this changes the name wherever the person appears, which
    /// includes exchanges organized by somebody else. That follows from a person being one row
    /// rather than one per membership, and it is the same reason an organizer editing a
    /// participant's name in <see cref="CreateParticipantAsync"/> is felt everywhere too.
    /// </remarks>
    public async Task UpdateOrganizerNameAsync(string organizerEmail, string name)
    {
        // As in ResolvePersonIdAsync: the empty address is the sentinel's, and this statement would
        // rename it rather than match nobody.
        if (string.IsNullOrWhiteSpace(organizerEmail))
            throw new ArgumentException("A person needs an email address; the empty one belongs to the sentinel row.", nameof(organizerEmail));

        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        await context.Persons
            .Where(person => person.Email == organizerEmail)
            .ExecuteUpdateAsync(setters => setters.SetProperty(person => person.Name, name))
            .ConfigureAwait(false);
    }

    public async Task<Participant> CreateParticipantAsync(
        AddParticipantRequest request,
        ImmutableList<Participant> existingParticipants
    )
    {
        var personId = await ResolvePersonIdAsync(request.Email, request.Name).ConfigureAwait(false);

        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var participant = new ParticipantEntity
        {
            ParticipantId = Guid.CreateVersion7(),
            HatId = request.HatId,
            PersonId = personId,
            PickedRecipientParticipantId = Guid.Empty
        };

        context.Participants.Add(participant);

        var existingEmails = existingParticipants
            .Select(existing => existing.Person.Email)
            .ToList();

        // The new participant may draw everyone already in the hat.
        var eligibleIds = await context.Participants
            .Where(candidate => candidate.HatId == request.HatId
                                && existingEmails.Contains(candidate.Person.Email))
            .Select(candidate => candidate.ParticipantId)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var eligibleId in eligibleIds)
            context.ParticipantEligibleRecipients.Add(new ParticipantEligibleRecipientEntity
            {
                ParticipantEligibleRecipientId = Guid.CreateVersion7(),
                ParticipantId = participant.ParticipantId,
                EligibleParticipantId = eligibleId
            });

        await context.SaveChangesAsync().ConfigureAwait(false);

        return new Participant
        {
            PickedRecipient = string.Empty,
            Person = new Person { Name = request.Name, Email = request.Email },
            EligibleRecipients = existingParticipants
                .Select(existing => existing.Person.Name)
                .ToImmutableList()
        };
    }

    public async Task<ImmutableList<Participant>> GetParticipantsAsync(string organizerEmail, Guid hatId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var participants = await LoadParticipantsAsync(context, organizerEmail, hatId).ConfigureAwait(false);

        return ToDomain(participants);
    }

    public async Task<(bool participantExists, Participant participant)> GetParticipantAsync(
        string requestOrganizerEmail,
        Guid requestHatId,
        string requestParticipantEmail
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        // The whole hat is loaded even though one participant is wanted. A pick is a participant id
        // and the domain record carries a name, so resolving it means having that participant in
        // hand; a hat holds a few dozen people at most, so fetching them all is cheaper than a
        // second round trip for the one being drawn.
        var participants = await LoadParticipantsAsync(context, requestOrganizerEmail, requestHatId)
            .ConfigureAwait(false);

        var participant = participants
            .SingleOrDefault(entity => entity.Person.Email == requestParticipantEmail);

        return participant is null
            ? (false, Participants.Empty)
            : (true, ToDomain(participant, NamesByParticipantId(participants)));
    }

    public async Task AddParticipantEligibleRecipientAsync(
        string organizerEmail,
        Guid hatId,
        string participantEmail,
        string recipientName
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var participantId = await FindParticipantIdByEmailAsync(context, hatId, participantEmail).ConfigureAwait(false);
        var recipientId = await FindParticipantIdByNameAsync(context, hatId, recipientName).ConfigureAwait(false);

        if (participantId == Guid.Empty || recipientId == Guid.Empty)
        {
            _logger.LogWarning("Could not add an eligible recipient to hat {HatId}: participant or recipient not found.", hatId);
            return;
        }

        context.ParticipantEligibleRecipients.Add(new ParticipantEligibleRecipientEntity
        {
            ParticipantEligibleRecipientId = Guid.CreateVersion7(),
            ParticipantId = participantId,
            EligibleParticipantId = recipientId
        });

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Already eligible. The unique index makes this idempotent rather than a duplicate row.
        }
    }

    /// <summary>
    /// Replaces a participant's eligibility list. An empty list is simply no rows.
    /// </summary>
    public Task UpdateEligibleRecipientsAsync(
        string organizerEmail,
        Guid hatId,
        string participantEmail,
        ImmutableList<string> eligibleRecipients
    ) =>
        InTransactionAsync(async context =>
        {
            var participantId = await FindParticipantIdByEmailAsync(context, hatId, participantEmail).ConfigureAwait(false);

            if (participantId == Guid.Empty)
            {
                _logger.LogWarning("Could not update eligible recipients for {ParticipantEmail}: not found in hat {HatId}.", participantEmail, hatId);
                return;
            }

            await context.ParticipantEligibleRecipients
                .Where(row => row.ParticipantId == participantId)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            var normalized = eligibleRecipients.Select(Normalize).ToList();

            // ToLower, not ToLowerInvariant: see FindParticipantIdByNameAsync.
            var recipientIds = await context.Participants
                .Where(candidate => candidate.HatId == hatId
                                    && normalized.Contains(candidate.Person.Name.ToLower()))
                .Select(candidate => candidate.ParticipantId)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var recipientId in recipientIds)
                context.ParticipantEligibleRecipients.Add(new ParticipantEligibleRecipientEntity
                {
                    ParticipantEligibleRecipientId = Guid.CreateVersion7(),
                    ParticipantId = participantId,
                    EligibleParticipantId = recipientId
                });

            await context.SaveChangesAsync().ConfigureAwait(false);
        });

    /// <summary>
    /// Removes a participant from a hat, along with their eligibility rows in both directions and
    /// any pick pointing at them. Without foreign keys nothing cleans up on our behalf, and a
    /// dangling picked_recipient_participant_id would survive the delete.
    ///
    /// The person stays. Leaving a hat is not ceasing to exist, and they may be in another one.
    /// </summary>
    public Task DeleteParticipantAsync(string requestOrganizerEmail, Guid requestHatId, string requestEmail) =>
        InTransactionAsync(async context =>
        {
            var participantId = await FindParticipantIdByEmailAsync(context, requestHatId, requestEmail).ConfigureAwait(false);

            if (participantId == Guid.Empty)
                return;

            await context.ParticipantEligibleRecipients
                .Where(row => row.ParticipantId == participantId
                              || row.EligibleParticipantId == participantId)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            await context.Participants
                .Where(participant => participant.PickedRecipientParticipantId == participantId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(participant => participant.PickedRecipientParticipantId, Guid.Empty))
                .ConfigureAwait(false);

            await context.Participants
                .Where(participant => participant.ParticipantId == participantId)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);
        });

    public async Task UpdateParticipantPickedRecipientAsync(
        string organizerEmail,
        Guid hatId,
        string participantEmail,
        string pickedRecipientName
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var participantId = await FindParticipantIdByEmailAsync(context, hatId, participantEmail).ConfigureAwait(false);

        if (participantId == Guid.Empty)
            return;

        // An empty name clears the pick, and so does a name nobody in the hat goes by — both end up
        // as the all-zero id, which is what "has not drawn" looks like.
        var pickedId = string.IsNullOrWhiteSpace(pickedRecipientName)
            ? Guid.Empty
            : await FindParticipantIdByNameAsync(context, hatId, pickedRecipientName).ConfigureAwait(false);

        await context.Participants
            .Where(participant => participant.ParticipantId == participantId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(participant => participant.PickedRecipientParticipantId, pickedId))
            .ConfigureAwait(false);
    }

    public async Task RemoveParticipantFromEligibleRecipientsAsync(
        string organizerEmail,
        Guid hatId,
        string participantNameToRemove
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var participantId = await FindParticipantIdByNameAsync(context, hatId, participantNameToRemove).ConfigureAwait(false);

        if (participantId == Guid.Empty)
            return;

        // Leaving someone with no eligible recipients is representable. It is a validation problem
        // for EligibilityValidationService to report, not a write that cannot be stored.
        await context.ParticipantEligibleRecipients
            .Where(row => row.EligibleParticipantId == participantId)
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);
    }

    public async Task<DateTimeOffset> MarkInvitationsAsQueuedAsync(
        string organizerEmail,
        Guid hatId,
        string sentFromIpAddress
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var invitationsQueuedAt = DateTimeOffset.UtcNow;

        var organizerPersonId = await FindPersonIdByEmailAsync(context, organizerEmail).ConfigureAwait(false);

        // An unknown address resolves to the all-zero id, which is the sentinel hat's organizer. It
        // could never satisfy the status check below, but a write scoped to "nobody" is not a write
        // worth attempting.
        if (organizerPersonId == Guid.Empty)
        {
            _logger.LogError("Hat {HatId} was not marked as queued: {OrganizerEmail} is not a known organizer.", hatId, organizerEmail);
            return invitationsQueuedAt;
        }

        // Conditional on the expected status, so two concurrent sends cannot both mark the hat.
        var updated = await context.Hats
            .Where(hat => hat.HatId == hatId
                          && hat.OrganizerPersonId == organizerPersonId
                          && hat.Status == HatStatus.NamesAssigned)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(hat => hat.Status, HatStatus.InvitationsSent)
                .SetProperty(hat => hat.InvitationsQueuedAt, invitationsQueuedAt)
                .SetProperty(hat => hat.InvitationsSentFromIp, sentFromIpAddress))
            .ConfigureAwait(false);

        if (updated == 0)
            _logger.LogError("Hat {HatId} was not in {ExpectedStatus} when invitations were queued, so it was left unchanged.", hatId, HatStatus.NamesAssigned);

        return invitationsQueuedAt;
    }

    public async Task TryTransitionHatToCooledOffAsync(string organizerEmail, Guid hatId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var organizerPersonId = await FindPersonIdByEmailAsync(context, organizerEmail).ConfigureAwait(false);

        if (organizerPersonId == Guid.Empty)
        {
            _logger.LogError("Couldn't update HatStatus to READY_TO_CLOSE for hat {hatId}: {OrganizerEmail} is not a known organizer.", hatId, organizerEmail);
            return;
        }

        var updated = await context.Hats
            .Where(hat => hat.HatId == hatId
                          && hat.OrganizerPersonId == organizerPersonId
                          && hat.Status == HatStatus.InvitationsSent)
            .ExecuteUpdateAsync(setters => setters.SetProperty(hat => hat.Status, HatStatus.CooledOff))
            .ConfigureAwait(false);

        if (updated == 0)
            _logger.LogError("Couldn't update HatStatus to READY_TO_CLOSE for hat {hatId}, since it does not have expected status. Will not retry.", hatId);
    }

    /// <summary>
    /// Every participant in a hat, with the person behind each one and the people they may draw.
    /// </summary>
    private static async Task<List<ParticipantEntity>> LoadParticipantsAsync(
        GiftExchangeDbContext context,
        string organizerEmail,
        Guid hatId
    ) =>
        await context.Participants
            .AsNoTracking()
            .Include(participant => participant.Person)
            .Include(participant => participant.EligibleRecipients)
            .ThenInclude(row => row.EligibleParticipant).ThenInclude(participant => participant.Person)
            .Where(participant => participant.HatId == hatId
                                  && participant.Hat.Organizer.Email == organizerEmail)
            .ToListAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// The person id registered to this email address, or <see cref="Guid.Empty"/> if nobody holds
    /// it. Absence is a value rather than a null here for the same reason it is in the schema.
    /// </summary>
    private static async Task<Guid> FindPersonIdByEmailAsync(GiftExchangeDbContext context, string email) =>
        await context.Persons
            .Where(person => person.Email == email)
            .Select(person => person.PersonId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// The id of the person registered to this email address, writing them in if they are new to
    /// the application, and recording the name the caller supplied.
    /// </summary>
    /// <remarks>
    /// Runs in its own context and its own transaction, ahead of whatever the caller is about to
    /// write, because a person is not part of any one exchange. That has two consequences worth
    /// stating.
    ///
    /// The first is that the race is handled here rather than left to the caller. Two requests can
    /// both find nobody and both try to write the same address; the unique index on person.email
    /// lets exactly one through, and the loser reads back the id the winner just created instead of
    /// failing. Introducing somebody twice at once is a collision with an obvious resolution, not
    /// an error to report.
    ///
    /// The second is that a person can outlive the write that introduced them — a hat rejected for
    /// a duplicate name leaves the organizer behind in the directory. A person with no hats and no
    /// participations is inert, and the next attempt finds them rather than writing them again.
    ///
    /// The supplied name always wins. An organizer adding somebody to an exchange, or editing them
    /// once they are in it, is stating what that person is called, and there is one place to say
    /// it. It follows that the change is visible in every hat they appear in — see
    /// <see cref="UpdateOrganizerNameAsync"/>.
    /// </remarks>
    private async Task<Guid> ResolvePersonIdAsync(string email, string name)
    {
        // The sentinel person holds the empty address. Reaching here with one would find it and
        // give it a name, and the caller would go on to write a hat owned by "nobody". Both are
        // worse than stopping: every address that gets this far has been through validation, so an
        // empty one is a bug upstream rather than input to accommodate.
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("A person needs an email address; the empty one belongs to the sentinel row.", nameof(email));

        await using (var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false))
        {
            var existing = await context.Persons
                .FirstOrDefaultAsync(candidate => candidate.Email == email)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                existing.Name = name;
                await context.SaveChangesAsync().ConfigureAwait(false);
                return existing.PersonId;
            }

            var person = new PersonEntity
            {
                PersonId = Guid.CreateVersion7(),
                Name = name,
                Email = email
            };

            context.Persons.Add(person);

            try
            {
                await context.SaveChangesAsync().ConfigureAwait(false);
                return person.PersonId;
            }
            catch (DbUpdateException exception) when (IsUniqueViolationOf(exception, PersonEmailIndex))
            {
                _logger.LogInformation("Another request introduced this person first; reading back the id it wrote.");
            }
        }

        // A fresh context, because the one above failed a save and still holds the person it could
        // not write.
        await using var reread = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        return await FindPersonIdByEmailAsync(reread, email).ConfigureAwait(false);
    }

    private static async Task<Guid> FindParticipantIdByEmailAsync(
        GiftExchangeDbContext context,
        Guid hatId,
        string email
    ) =>
        await context.Participants
            .Where(participant => participant.HatId == hatId && participant.Person.Email == email)
            .Select(participant => participant.ParticipantId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

    /// <summary>
    /// Name lookups exist because the domain records still identify participants by name. They
    /// are only unambiguous because AddParticipantService refuses duplicate names within a hat.
    /// </summary>
    /// <remarks>
    /// ToLower rather than ToLowerInvariant, and deliberately so. Inside a LINQ expression tree
    /// this is never executed as a .NET string operation: EF translates it to SQL lower() and
    /// Postgres does the folding under the column's collation, so there is no .NET culture
    /// involved to get wrong. ToLowerInvariant has no translation at all and throws at query time.
    /// Use ToLowerInvariant everywhere the comparison really does happen in memory — see
    /// <see cref="Normalize"/>.
    /// </remarks>
    private static async Task<Guid> FindParticipantIdByNameAsync(
        GiftExchangeDbContext context,
        Guid hatId,
        string name
    )
    {
        var normalized = Normalize(name);

        return await context.Participants
            .Where(participant => participant.HatId == hatId
                                  && participant.Person.Name.ToLower() == normalized)
            .Select(participant => participant.ParticipantId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    private static ImmutableList<Participant> ToDomain(ICollection<ParticipantEntity> participants)
    {
        var namesByParticipantId = NamesByParticipantId(participants);

        return participants
            .Select(participant => ToDomain(participant, namesByParticipantId))
            .ToImmutableList();
    }

    /// <summary>
    /// A pick is stored as a participant id and the domain record carries a name, so translating
    /// one needs the rest of the hat. There is no navigation to follow instead: a reference
    /// navigation would have made EF emit a foreign key, and the all-zero id standing for "has not
    /// drawn" would fail it.
    /// </summary>
    private static Dictionary<Guid, string> NamesByParticipantId(ICollection<ParticipantEntity> participants) =>
        participants.ToDictionary(
            participant => participant.ParticipantId,
            participant => participant.Person.Name);

    private static Participant ToDomain(
        ParticipantEntity participant,
        IReadOnlyDictionary<Guid, string> namesByParticipantId
    ) =>
        new()
        {
            // Guid.Empty is in no hat, so an undrawn participant falls through to the empty name
            // without being asked about separately.
            PickedRecipient = namesByParticipantId.GetValueOrDefault(
                participant.PickedRecipientParticipantId,
                string.Empty),
            Person = new Person { Name = participant.Person.Name, Email = participant.Person.Email },
            EligibleRecipients = participant.EligibleRecipients
                .Select(row => row.EligibleParticipant.Person.Name)
                .ToImmutableList()
        };

    private static string Normalize(string value) => value.TrimNullSafe().ToLowerInvariant();

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };

    /// <summary>
    /// A unique violation on one named index. Catching the index by name keeps a collision the
    /// caller can act on apart from one it cannot.
    /// </summary>
    private static bool IsUniqueViolationOf(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation } postgres
        && postgres.ConstraintName == constraintName;

    /// <summary>
    /// Runs several statements as one unit, retried as a whole if the strategy decides to.
    ///
    /// The execution strategy wrapper is required because retries are enabled: EF refuses a
    /// user-initiated transaction otherwise, since it cannot safely replay one it did not open.
    ///
    /// Every attempt gets its own context. Sharing one would leave entities from a failed attempt
    /// sitting in the change tracker as Added, so a retry would insert them a second time and
    /// collide with the unique index. It also keeps the retried work from closing over a context
    /// whose lifetime is owned by this method.
    /// </summary>
    internal async Task InTransactionAsync(Func<GiftExchangeDbContext, Task> work)
    {
        // Exists only to read the configured strategy, which comes from the provider rather than
        // from any connection. It stays alive for the duration because the strategy uses it for
        // diagnostics while executing.
        await using var strategyContext = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var strategy = strategyContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
            // REPEATABLE READ is stated explicitly because it is the only level DSQL accepts.
            // Npgsql translates the default into READ COMMITTED, which DSQL rejects outright with
            // "0A000: Unsupported isolation level". Postgres accepts both, so the test suite cannot
            // catch this — it was only visible against a real cluster.
            await using var transaction = await context.Database
                .BeginTransactionAsync(IsolationLevel.RepeatableRead)
                .ConfigureAwait(false);

            await work(context).ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}
