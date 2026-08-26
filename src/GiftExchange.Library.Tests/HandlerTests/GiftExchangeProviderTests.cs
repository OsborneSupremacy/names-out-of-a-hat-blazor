using System.Data;
using GiftExchange.Library.Contexts;
using GiftExchange.Library.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace GiftExchange.Library.Tests.HandlerTests;

[Collection(PostgresCollection.Name)]
public class GiftExchangeProviderTests
{
    private readonly GiftExchangeProvider _sut;

    private readonly HatDataModelFaker _hatDataModelFaker;

    private readonly AddParticipantRequestFaker _addParticipantRequestFaker;

    private readonly IDbContextFactory<GiftExchangeDbContext> _contextFactory;

    private readonly PostgresFixture _dbFixture;

    public GiftExchangeProviderTests(PostgresFixture dbFixture)
    {
        DotEnv.Load();

        _hatDataModelFaker = new HatDataModelFaker();
        _addParticipantRequestFaker = new AddParticipantRequestFaker();

        var contextFactory = dbFixture.CreateContextFactory();

        var serviceProvider = new ServiceCollection()
            .AddUtilities()
            .AddBusinessServices()
            .AddSingleton(contextFactory)
            .BuildServiceProvider();

        _sut = serviceProvider.GetRequiredService<GiftExchangeProvider>();
        _contextFactory = contextFactory;
        _dbFixture = dbFixture;
    }

    [Fact]
    public async Task CreateHatAsync_GivenValidPayload_ShouldCreateHat()
    {
        // arrange
        var hat = _hatDataModelFaker.Generate();

        // act
        var result = await _sut.CreateHatAsync(hat);

        // assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrganizerHatsAsync_GivenExistingOrganizerEmail_ShouldReturnHats()
    {
        // arrange
        var hats = _hatDataModelFaker.Generate(2);

        var hatOne = hats[0];
        var hatTwo = hats[1] with
        {
            OrganizerEmail = hatOne.OrganizerEmail,
            OrganizerName = hatOne.OrganizerName
        };

        await _sut.CreateHatAsync(hatOne);
        await _sut.CreateHatAsync(hatTwo);

        // act
        var (organizerName, result) = await _sut.GetHatsAsync(hatOne.OrganizerEmail);

        // assert
        organizerName.Should().Be(hatOne.OrganizerName);
        result.Should().BeEquivalentTo([
            new HatMetaData { HatId = hatOne.HatId, HatName = hatOne.HatName, Status = HatStatus.InProgress },
            new HatMetaData { HatId = hatTwo.HatId, HatName = hatTwo.HatName, Status = HatStatus.InProgress }
        ]);
    }

    [Fact]
    public async Task CreateParticipantAsync_GivenValidPayload_ShouldCreateParticipant()
    {
        // arrange
        var hat = await CreateHatAsync();
        var request = ParticipantRequestFor(hat);

        // act
        var result = await _sut.CreateParticipantAsync(request, []);

        // assert
        result.Should().BeOfType<Participant>();
    }

    [Fact]
    public async Task CreateParticipantAsync_GivenDuplicatePayload_ShouldThrowException()
    {
        // arrange
        var hat = await CreateHatAsync();
        var request = ParticipantRequestFor(hat);

        // act
        var firstRequest = await _sut.CreateParticipantAsync(request, []);

        Func<Task> secondRequest = async () => await _sut.CreateParticipantAsync(request, []);

        // assert
        firstRequest.Should().BeOfType<Participant>();
        await secondRequest.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetParticipantsAsync_GivenExistingParticipants_ShouldReturnParticipants()
    {
        // arrange
        var hat = await CreateHatAsync();

        foreach (var participant in _addParticipantRequestFaker.Generate(3))
            await _sut.CreateParticipantAsync(ParticipantRequestFor(hat, participant), []);

        // act
        var result = await _sut.GetParticipantsAsync(hat.OrganizerEmail, hat.HatId);

        // assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public async Task CreateParticipantAsync_GivenExistingParticipants_ShouldMakeThemEligible()
    {
        // arrange
        var hat = await CreateHatAsync();

        var first = await _sut.CreateParticipantAsync(ParticipantRequestFor(hat), []);
        var second = await _sut.CreateParticipantAsync(ParticipantRequestFor(hat), [first]);

        // act
        var (exists, stored) = await _sut.GetParticipantAsync(hat.OrganizerEmail, hat.HatId, second.Person.Email);

        // assert: eligibility is stored by id and read back as a name.
        exists.Should().BeTrue();
        stored.EligibleRecipients.Should().ContainSingle().Which.Should().Be(first.Person.Name);
    }

    [Fact]
    public async Task UpdateEligibleRecipientsAsync_GivenAnEmptyList_ClearsThemWithoutFailing()
    {
        // arrange
        var hat = await CreateHatAsync();
        var alpha = await _sut.CreateParticipantAsync(ParticipantRequestFor(hat), []);
        var beta = await _sut.CreateParticipantAsync(ParticipantRequestFor(hat), [alpha]);

        // act: DynamoDB rejects an empty string set outright, so this used to be unrepresentable.
        var act = async () => await _sut.UpdateEligibleRecipientsAsync(
            hat.OrganizerEmail, hat.HatId, beta.Person.Email, []);

        // assert
        await act.Should().NotThrowAsync();

        var (_, stored) = await _sut.GetParticipantAsync(hat.OrganizerEmail, hat.HatId, beta.Person.Email);
        stored.EligibleRecipients.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveParticipantFromEligibleRecipientsAsync_WhenItLeavesSomeoneWithNone_Succeeds()
    {
        // arrange
        var hat = await CreateHatAsync();
        var alpha = await _sut.CreateParticipantAsync(ParticipantRequestFor(hat), []);
        var beta = await _sut.CreateParticipantAsync(ParticipantRequestFor(hat), [alpha]);

        await _sut.UpdateEligibleRecipientsAsync(
            hat.OrganizerEmail, hat.HatId, beta.Person.Email, [alpha.Person.Name]);

        // act: removing Alpha leaves Beta with nobody to draw. Writing that back to DynamoDB meant
        // an empty string set, which it refuses, so the request failed with a 500 partway through.
        var act = async () => await _sut.RemoveParticipantFromEligibleRecipientsAsync(
            hat.OrganizerEmail, hat.HatId, alpha.Person.Name);

        // assert
        await act.Should().NotThrowAsync();

        var (_, stored) = await _sut.GetParticipantAsync(hat.OrganizerEmail, hat.HatId, beta.Person.Email);
        stored.EligibleRecipients.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteParticipantAsync_ClearsAnyPickPointingAtThem()
    {
        // arrange
        var hat = await CreateHatAsync();
        var receiver = await _sut.CreateParticipantAsync(ParticipantRequestFor(hat), []);
        var giver = await _sut.CreateParticipantAsync(ParticipantRequestFor(hat), [receiver]);

        await _sut.UpdateParticipantPickedRecipientAsync(
            hat.OrganizerEmail, hat.HatId, giver.Person.Email, receiver.Person.Name);

        // act: without foreign keys nothing cascades, so the pick has to be cleared explicitly or
        // it dangles at a participant who no longer exists.
        await _sut.DeleteParticipantAsync(hat.OrganizerEmail, hat.HatId, receiver.Person.Email);

        // assert
        var (exists, stored) = await _sut.GetParticipantAsync(hat.OrganizerEmail, hat.HatId, giver.Person.Email);
        exists.Should().BeTrue();
        stored.PickedRecipient.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChanges_OpensItsOwnImplicitTransactionAtRepeatableRead()
    {
        // arrange: this is the exact path that failed against DSQL. A SaveChanges with more than
        // one statement makes EF open a transaction nobody asked for, at Npgsql's default of READ
        // COMMITTED. Postgres accepts that, so only the recorded level shows whether the
        // interceptor reached it.
        var recorder = new RecordingTransactionInterceptor();
        var contextFactory = _dbFixture.CreateContextFactory(recorder);

        var hat = _hatDataModelFaker.Generate();
        await _sut.CreateHatAsync(hat);

        await using var context = contextFactory.CreateDbContext();

        context.Participants.AddRange(
            new ParticipantEntity
            {
                Id = Guid.CreateVersion7(), HatId = hat.HatId, Name = "First", Email = "first@example.com"
            },
            new ParticipantEntity
            {
                Id = Guid.CreateVersion7(), HatId = hat.HatId, Name = "Second", Email = "second@example.com"
            });

        // act
        await context.SaveChangesAsync();

        // assert
        recorder.StartedLevels.Should().NotBeEmpty("a multi-statement SaveChanges opens a transaction");
        recorder.StartedLevels.Should().AllSatisfy(level => level.Should().Be(IsolationLevel.RepeatableRead));
    }

    [Fact]
    public async Task Transactions_AreUpgradedToRepeatableReadEvenWhenSomethingAsksForReadCommitted()
    {
        // arrange: EF opens transactions nobody asked for — SaveChanges wraps a multi-statement
        // batch in one, at Npgsql's default of READ COMMITTED, which DSQL rejects. There is no
        // overload to influence that, so an interceptor upgrades every transaction. Asking for
        // READ COMMITTED explicitly is the observable stand-in for what EF does internally.
        await using var context = _contextFactory.CreateDbContext();

        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        // act
        var connection = context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SHOW transaction_isolation";

        var isolationLevel = (string?)await command.ExecuteScalarAsync();

        // assert
        isolationLevel.Should().Be("repeatable read");
    }

    [Fact]
    public async Task InTransactionAsync_OpensTheTransactionAtRepeatableRead()
    {
        // arrange: DSQL accepts no other isolation level, and Npgsql's default is READ COMMITTED,
        // which it rejects. Postgres tolerates both, so asserting the level is the only way this
        // suite can protect a DSQL-only requirement.
        string? isolationLevel = null;

        // act
        await _sut.InTransactionAsync(async context =>
        {
            var connection = context.Database.GetDbConnection();

            await using var command = connection.CreateCommand();
            command.Transaction = context.Database.CurrentTransaction!.GetDbTransaction();
            command.CommandText = "SHOW transaction_isolation";

            isolationLevel = (string?)await command.ExecuteScalarAsync();
        });

        // assert
        isolationLevel.Should().Be("repeatable read");
    }

    /// <summary>
    /// Participants belong to a hat. DynamoDB tolerated orphans; a relational schema does not, so
    /// the hat has to exist first.
    /// </summary>
    private async Task<HatDataModel> CreateHatAsync()
    {
        var hat = _hatDataModelFaker.Generate();
        await _sut.CreateHatAsync(hat);
        return hat;
    }

    private AddParticipantRequest ParticipantRequestFor(HatDataModel hat, AddParticipantRequest? request = null)
    {
        request ??= _addParticipantRequestFaker.Generate();

        return request with { HatId = hat.HatId, OrganizerEmail = hat.OrganizerEmail };
    }
}
