using System.Data;
using GiftExchange.Library.Contexts;
using Npgsql;

namespace GiftExchange.Library.Providers;

/// <summary>
/// Data access for gift exchanges.
///
/// The public surface still speaks the domain records, which identify participants by display
/// name. Storage no longer does: eligibility and picked recipients are foreign keys, and this
/// class translates at the boundary. That containment is deliberate — the API contract still
/// carries names, so changing it is a separate job.
/// </summary>
[UsedImplicitly]
public class GiftExchangeProvider
{
    /// <summary>Postgres unique_violation.</summary>
    private const string UniqueViolation = "23505";

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

        var hats = await context.Hats
            .AsNoTracking()
            .Where(hat => hat.OrganizerEmail == organizerEmail)
            .OrderByDescending(hat => hat.CreatedAt)
            .Select(hat => new { hat.Id, hat.Name, hat.Status, hat.OrganizerName })
            .ToListAsync()
            .ConfigureAwait(false);

        // Newest first, so this is the name the organizer used most recently.
        var organizerName = hats
            .Select(hat => hat.OrganizerName)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty;

        return (
            organizerName,
            hats.Select(hat => new HatMetaData
            {
                HatId = hat.Id,
                HatName = hat.Name,
                Status = hat.Status
            }).ToImmutableList()
        );
    }

    /// <returns>true if the hat was created, false if the organizer already has one by that name.</returns>
    public async Task<bool> CreateHatAsync(HatDataModel hatDataModel)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        context.Hats.Add(new HatEntity
        {
            Id = hatDataModel.HatId,
            OrganizerEmail = hatDataModel.OrganizerEmail,
            OrganizerName = hatDataModel.OrganizerName,
            Name = hatDataModel.HatName,
            NameNormalized = Normalize(hatDataModel.HatName),
            Status = hatDataModel.Status,
            AdditionalInformation = hatDataModel.AdditionalInformation,
            PriceRange = hatDataModel.PriceRange,
            CreatedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
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
    /// Participants are re-created with new ids rather than reused, and the eligibility rows are
    /// translated through that mapping. Matching on name would have been simpler and wrong: two
    /// people in a hat may share a display name.
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
            await InTransactionAsync(async context =>
            {
                var source = await context.Hats
                    .AsNoTracking()
                    .Include(hat => hat.Participants).ThenInclude(participant => participant.EligibleRecipients)
                    .SingleAsync(hat => hat.Id == sourceHatId && hat.OrganizerEmail == newHat.OrganizerEmail)
                    .ConfigureAwait(false);

                context.Hats.Add(new HatEntity
                {
                    Id = newHat.HatId,
                    OrganizerEmail = newHat.OrganizerEmail,
                    OrganizerName = newHat.OrganizerName,
                    Name = newHat.HatName,
                    NameNormalized = Normalize(newHat.HatName),
                    Status = newHat.Status,
                    AdditionalInformation = newHat.AdditionalInformation,
                    PriceRange = newHat.PriceRange,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                var newParticipantIds = source.Participants
                    .ToDictionary(participant => participant.Id, _ => Guid.CreateVersion7());

                foreach (var participant in source.Participants)
                    context.Participants.Add(new ParticipantEntity
                    {
                        Id = newParticipantIds[participant.Id],
                        HatId = newHat.HatId,
                        Name = participant.Name,
                        Email = participant.Email
                        // PickedRecipientId stays null. A copy has not been shaken, which is the
                        // whole point of making one.
                    });

                foreach (var participant in source.Participants)
                foreach (var eligibility in participant.EligibleRecipients)
                {
                    if (excludePreviousRecipients && eligibility.EligibleParticipantId == participant.PickedRecipientId)
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
                        ParticipantEligibleRecipientsId = Guid.CreateVersion7(),
                        ParticipantId = newParticipantIds[participant.Id],
                        EligibleParticipantId = eligibleId
                    });
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);

            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // The caller checks the name first; this closes the window between that check and
            // this write, as it does for a hat created from scratch.
            return false;
        }
    }

    public async Task<(bool exists, Guid hatId)> DoesHatAlreadyExistAsync(string organizerEmail, string hatName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var normalized = Normalize(hatName);

        var hatId = await context.Hats
            .AsNoTracking()
            .Where(hat => hat.OrganizerEmail == organizerEmail && hat.NameNormalized == normalized)
            .Select(hat => (Guid?)hat.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        return hatId is null ? (false, Guid.Empty) : (true, hatId.Value);
    }

    public async Task<(bool exists, Hat hat)> GetHatAsync(string organizerEmail, Guid hatId)
    {
        if (string.IsNullOrWhiteSpace(organizerEmail) || hatId == Guid.Empty)
            return (false, Hats.Empty);

        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var hat = await context.Hats
            .AsNoTracking()
            .Include(entity => entity.Participants).ThenInclude(participant => participant.PickedRecipient)
            .Include(entity => entity.Participants).ThenInclude(participant => participant.EligibleRecipients)
            .ThenInclude(eligible => eligible.EligibleParticipant)
            .SingleOrDefaultAsync(entity => entity.Id == hatId && entity.OrganizerEmail == organizerEmail)
            .ConfigureAwait(false);

        if (hat is null)
            return (false, Hats.Empty);

        return (true, new Hat
        {
            Id = hat.Id,
            Name = hat.Name,
            Status = hat.Status,
            AdditionalInformation = hat.AdditionalInformation,
            PriceRange = hat.PriceRange,
            Organizer = new Person { Name = hat.OrganizerName, Email = hat.OrganizerEmail },
            Participants = hat.Participants.Select(ToDomain).ToImmutableList(),
            InvitationsQueuedDate = hat.InvitationsQueuedAt ?? DateTimeOffset.MinValue
        });
    }

    public async Task EditHatAsync(EditHatRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        await context.Hats
            .Where(hat => hat.Id == request.HatId && hat.OrganizerEmail == request.OrganizerEmail)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(hat => hat.Name, request.Name)
                .SetProperty(hat => hat.NameNormalized, Normalize(request.Name))
                .SetProperty(hat => hat.AdditionalInformation, request.AdditionalInformation)
                .SetProperty(hat => hat.PriceRange, request.PriceRange))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the hat and everything hanging off it. DSQL has no ON DELETE CASCADE, so the
    /// order is explicit, and the whole thing runs in one transaction — the DynamoDB version
    /// issued independent deletes and could leave a half-removed hat behind.
    /// </summary>
    public Task DeleteHatAsync(DeleteHatRequest request) =>
        InTransactionAsync(async context =>
        {
            var participantIds = context.Participants
                .Where(participant => participant.HatId == request.HatId)
                .Select(participant => participant.Id);

            await context.ParticipantEligibleRecipients
                .Where(row => participantIds.Contains(row.ParticipantId)
                              || participantIds.Contains(row.EligibleParticipantId))
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            await context.Participants
                .Where(participant => participant.HatId == request.HatId)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            await context.Hats
                .Where(hat => hat.Id == request.HatId && hat.OrganizerEmail == request.OrganizerEmail)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);
        });

    public async Task UpdateHatStatusAsync(string organizerEmail, Guid hatId, string newStatus)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        await context.Hats
            .Where(hat => hat.Id == hatId && hat.OrganizerEmail == organizerEmail)
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

        var normalized = Normalize(name);

        var hatNames = await context.Hats
            .AsNoTracking()
            // ToLower, not ToLowerInvariant: see FindParticipantIdByNameAsync.
            .Where(hat => hat.OrganizerEmail == organizerEmail
                          && hat.Participants.Any(participant => participant.Email != organizerEmail
                                                                 && participant.Name.ToLower() == normalized))
            .Select(hat => hat.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        return hatNames.ToImmutableList();
    }

    /// <summary>
    /// Renames the organizer everywhere their name is stored: on each of their hats, and on their
    /// own participant row within them. Both in one transaction, so the two never disagree.
    ///
    /// Scoped to hats they own. Their email may also appear as a participant in somebody else's
    /// exchange, and renaming themselves there would change another organizer's data underneath
    /// them.
    /// </summary>
    public Task UpdateOrganizerNameAsync(string organizerEmail, string name) =>
        InTransactionAsync(async context =>
        {
            await context.Hats
                .Where(hat => hat.OrganizerEmail == organizerEmail)
                .ExecuteUpdateAsync(setters => setters.SetProperty(hat => hat.OrganizerName, name))
                .ConfigureAwait(false);

            var ownHatIds = context.Hats
                .Where(hat => hat.OrganizerEmail == organizerEmail)
                .Select(hat => hat.Id);

            await context.Participants
                .Where(participant => participant.Email == organizerEmail
                                      && ownHatIds.Contains(participant.HatId))
                .ExecuteUpdateAsync(setters => setters.SetProperty(participant => participant.Name, name))
                .ConfigureAwait(false);
        });

    public async Task<Participant> CreateParticipantAsync(
        AddParticipantRequest request,
        ImmutableList<Participant> existingParticipants
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var participant = new ParticipantEntity
        {
            Id = Guid.CreateVersion7(),
            HatId = request.HatId,
            Name = request.Name,
            Email = request.Email
        };

        context.Participants.Add(participant);

        var existingEmails = existingParticipants
            .Select(existing => existing.Person.Email)
            .ToList();

        // The new participant may draw everyone already in the hat.
        var eligibleIds = await context.Participants
            .Where(candidate => candidate.HatId == request.HatId && existingEmails.Contains(candidate.Email))
            .Select(candidate => candidate.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var eligibleId in eligibleIds)
            context.ParticipantEligibleRecipients.Add(new ParticipantEligibleRecipientEntity
            {
                ParticipantEligibleRecipientsId = Guid.CreateVersion7(),
                ParticipantId = participant.Id,
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

        var participants = await QueryParticipants(context, organizerEmail, hatId)
            .ToListAsync()
            .ConfigureAwait(false);

        return participants.Select(ToDomain).ToImmutableList();
    }

    public async Task<(bool participantExists, Participant participant)> GetParticipantAsync(
        string requestOrganizerEmail,
        Guid requestHatId,
        string requestParticipantEmail
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var participant = await QueryParticipants(context, requestOrganizerEmail, requestHatId)
            .SingleOrDefaultAsync(entity => entity.Email == requestParticipantEmail)
            .ConfigureAwait(false);

        return participant is null
            ? (false, Participants.Empty)
            : (true, ToDomain(participant));
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

        if (participantId is null || recipientId is null)
        {
            _logger.LogWarning("Could not add an eligible recipient to hat {HatId}: participant or recipient not found.", hatId);
            return;
        }

        context.ParticipantEligibleRecipients.Add(new ParticipantEligibleRecipientEntity
        {
            ParticipantEligibleRecipientsId = Guid.CreateVersion7(),
            ParticipantId = participantId.Value,
            EligibleParticipantId = recipientId.Value
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
    /// Replaces a participant's eligibility list. An empty list is simply no rows — the DynamoDB
    /// version wrote an empty string set here, which DynamoDB rejects outright, so removing a
    /// participant who was someone's only eligible recipient used to fail with a 500.
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

            if (participantId is null)
            {
                _logger.LogWarning("Could not update eligible recipients for {ParticipantEmail}: not found in hat {HatId}.", participantEmail, hatId);
                return;
            }

            await context.ParticipantEligibleRecipients
                .Where(row => row.ParticipantId == participantId.Value)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            var normalized = eligibleRecipients.Select(Normalize).ToList();

            // ToLower, not ToLowerInvariant: see FindParticipantIdByNameAsync.
            var recipientIds = await context.Participants
                .Where(candidate => candidate.HatId == hatId && normalized.Contains(candidate.Name.ToLower()))
                .Select(candidate => candidate.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var recipientId in recipientIds)
                context.ParticipantEligibleRecipients.Add(new ParticipantEligibleRecipientEntity
                {
                    ParticipantEligibleRecipientsId = Guid.CreateVersion7(),
                    ParticipantId = participantId.Value,
                    EligibleParticipantId = recipientId
                });

            await context.SaveChangesAsync().ConfigureAwait(false);
        });

    /// <summary>
    /// Removes a participant, their eligibility rows in both directions, and any pick pointing at
    /// them. Without foreign keys nothing cleans up on our behalf, and a dangling
    /// picked_recipient_id would survive the delete.
    /// </summary>
    public Task DeleteParticipantAsync(string requestOrganizerEmail, Guid requestHatId, string requestEmail) =>
        InTransactionAsync(async context =>
        {
            var participantId = await FindParticipantIdByEmailAsync(context, requestHatId, requestEmail).ConfigureAwait(false);

            if (participantId is null)
                return;

            await context.ParticipantEligibleRecipients
                .Where(row => row.ParticipantId == participantId.Value
                              || row.EligibleParticipantId == participantId.Value)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            await context.Participants
                .Where(participant => participant.PickedRecipientId == participantId.Value)
                .ExecuteUpdateAsync(setters => setters.SetProperty(participant => participant.PickedRecipientId, (Guid?)null))
                .ConfigureAwait(false);

            await context.Participants
                .Where(participant => participant.Id == participantId.Value)
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

        if (participantId is null)
            return;

        var pickedId = string.IsNullOrWhiteSpace(pickedRecipientName)
            ? null
            : await FindParticipantIdByNameAsync(context, hatId, pickedRecipientName).ConfigureAwait(false);

        await context.Participants
            .Where(participant => participant.Id == participantId.Value)
            .ExecuteUpdateAsync(setters => setters.SetProperty(participant => participant.PickedRecipientId, pickedId))
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

        if (participantId is null)
            return;

        // Leaving someone with no eligible recipients is now representable. It is a validation
        // problem for EligibilityValidationService to report, not a write that cannot be stored.
        await context.ParticipantEligibleRecipients
            .Where(row => row.EligibleParticipantId == participantId.Value)
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

        // Conditional on the expected status, so two concurrent sends cannot both mark the hat.
        var updated = await context.Hats
            .Where(hat => hat.Id == hatId
                          && hat.OrganizerEmail == organizerEmail
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

        var updated = await context.Hats
            .Where(hat => hat.Id == hatId
                          && hat.OrganizerEmail == organizerEmail
                          && hat.Status == HatStatus.InvitationsSent)
            .ExecuteUpdateAsync(setters => setters.SetProperty(hat => hat.Status, HatStatus.CooledOff))
            .ConfigureAwait(false);

        if (updated == 0)
            _logger.LogError("Couldn't update HatStatus to READY_TO_CLOSE for hat {hatId}, since it does not have expected status. Will not retry.", hatId);
    }

    private static IQueryable<ParticipantEntity> QueryParticipants(
        GiftExchangeDbContext context,
        string organizerEmail,
        Guid hatId
    ) =>
        context.Participants
            .AsNoTracking()
            .Include(participant => participant.PickedRecipient)
            .Include(participant => participant.EligibleRecipients).ThenInclude(row => row.EligibleParticipant)
            .Where(participant => participant.HatId == hatId
                                  && participant.Hat.OrganizerEmail == organizerEmail);

    private static async Task<Guid?> FindParticipantIdByEmailAsync(
        GiftExchangeDbContext context,
        Guid hatId,
        string email
    ) =>
        await context.Participants
            .Where(participant => participant.HatId == hatId && participant.Email == email)
            .Select(participant => (Guid?)participant.Id)
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
    private static async Task<Guid?> FindParticipantIdByNameAsync(
        GiftExchangeDbContext context,
        Guid hatId,
        string name
    )
    {
        var normalized = Normalize(name);

        return await context.Participants
            .Where(participant => participant.HatId == hatId && participant.Name.ToLower() == normalized)
            .Select(participant => (Guid?)participant.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    private static Participant ToDomain(ParticipantEntity participant) =>
        new()
        {
            PickedRecipient = participant.PickedRecipient?.Name ?? string.Empty,
            Person = new Person { Name = participant.Name, Email = participant.Email },
            EligibleRecipients = participant.EligibleRecipients
                .Select(row => row.EligibleParticipant.Name)
                .ToImmutableList()
        };

    private static string Normalize(string value) => value.TrimNullSafe().ToLowerInvariant();

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };

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
