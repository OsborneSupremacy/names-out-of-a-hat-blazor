using GiftExchange.Library.Contexts;
using GiftExchange.Library.Entities;
using Microsoft.Data.Sqlite;

namespace GiftExchange.Library.Tests.EntityTests;

/// <summary>
/// Proves the entities actually materialise, which model building alone does not. SQLite in
/// memory stands in for DSQL here: it is a real relational provider, so it exercises EF's
/// materialiser and relationship fixup, but it is not a substitute for testing against a cluster.
/// The point is the mapping mechanics, not DSQL semantics.
/// </summary>
public class EntityRoundTripTests : IDisposable
{
    private readonly SqliteConnection _connection;

    private readonly DbContextOptions<GiftExchangeDbContext> _options;

    public EntityRoundTripTests()
    {
        // An in-memory SQLite database lives only as long as its connection is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<GiftExchangeDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new GiftExchangeDbContext(_options);
        context.Database.EnsureCreated();
    }

    private GiftExchangeDbContext CreateContext() => new(_options);

    [Fact]
    public async Task Entities_WithRequiredMembers_RoundTripThroughEfCore()
    {
        var hatId = Guid.CreateVersion7();
        var organizerId = Guid.CreateVersion7();
        var participantId = Guid.CreateVersion7();

        await using (var context = CreateContext())
        {
            context.HatStatuses.Add(new HatStatusEntity { Status = HatStatus.InProgress });

            context.Hats.Add(new HatEntity
            {
                Id = hatId,
                OrganizerEmail = "organizer@example.com",
                OrganizerName = "Organizer",
                Name = "Family Christmas",
                NameNormalized = "family christmas",
                Status = HatStatus.InProgress,
                CreatedAt = DateTimeOffset.UtcNow
            });

            context.Participants.AddRange(
                new ParticipantEntity
                {
                    Id = organizerId, HatId = hatId, Name = "Organizer", Email = "organizer@example.com"
                },
                new ParticipantEntity
                {
                    Id = participantId, HatId = hatId, Name = "Someone Else", Email = "someone@example.com"
                });

            context.ParticipantEligibleRecipients.Add(new ParticipantEligibleRecipientEntity
            {
                ParticipantEligibleRecipientsId = Guid.CreateVersion7(),
                ParticipantId = organizerId,
                EligibleParticipantId = participantId
            });

            await context.SaveChangesAsync();
        }

        // A fresh context, so this reads from the database rather than the change tracker.
        await using (var context = CreateContext())
        {
            var hat = await context.Hats
                .Include(h => h.Participants)
                .ThenInclude(p => p.EligibleRecipients)
                .SingleAsync(h => h.Id == hatId);

            hat.Name.Should().Be("Family Christmas");
            hat.InvitationsQueuedAt.Should().BeNull();
            hat.Participants.Should().HaveCount(2);
            hat.Participants.Single(p => p.Id == organizerId).EligibleRecipients.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task PickedRecipient_ResolvesAsASelfReference()
    {
        var hatId = Guid.CreateVersion7();
        var giverId = Guid.CreateVersion7();
        var receiverId = Guid.CreateVersion7();

        await using (var context = CreateContext())
        {
            context.HatStatuses.Add(new HatStatusEntity { Status = HatStatus.NamesAssigned });
            context.Hats.Add(new HatEntity
            {
                Id = hatId,
                OrganizerEmail = "organizer@example.com",
                OrganizerName = "Organizer",
                Name = "Shaken Hat",
                NameNormalized = "shaken hat",
                Status = HatStatus.NamesAssigned,
                CreatedAt = DateTimeOffset.UtcNow
            });
            context.Participants.AddRange(
                new ParticipantEntity { Id = receiverId, HatId = hatId, Name = "Receiver", Email = "receiver@example.com" },
                new ParticipantEntity
                {
                    Id = giverId, HatId = hatId, Name = "Giver", Email = "giver@example.com",
                    PickedRecipientId = receiverId
                });

            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var giver = await context.Participants
                .Include(p => p.PickedRecipient)
                .SingleAsync(p => p.Id == giverId);

            giver.PickedRecipient.Should().NotBeNull();
            giver.PickedRecipient!.Name.Should().Be("Receiver");
        }
    }

    [Fact]
    public async Task TrackedEntity_CanBeMutatedAndSaved()
    {
        var hatId = Guid.CreateVersion7();

        await using (var context = CreateContext())
        {
            context.HatStatuses.AddRange(
                new HatStatusEntity { Status = HatStatus.InProgress },
                new HatStatusEntity { Status = HatStatus.Closed });

            context.Hats.Add(new HatEntity
            {
                Id = hatId,
                OrganizerEmail = "organizer@example.com",
                OrganizerName = "Organizer",
                Name = "Mutable Hat",
                NameNormalized = "mutable hat",
                Status = HatStatus.InProgress,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // This is why the properties are `set` rather than `init`: EF's update path is to load,
        // mutate, and save. Init-only properties would not compile here.
        await using (var context = CreateContext())
        {
            var hat = await context.Hats.SingleAsync(h => h.Id == hatId);
            hat.Status = HatStatus.Closed;
            hat.InvitationsQueuedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var hat = await context.Hats.SingleAsync(h => h.Id == hatId);
            hat.Status.Should().Be(HatStatus.Closed);
            hat.InvitationsQueuedAt.Should().NotBeNull();
        }
    }

    public void Dispose() => _connection.Dispose();
}
