using System.Text.RegularExpressions;
using GiftExchange.Library.Contexts;
using GiftExchange.Library.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GiftExchange.Library.Tests.EntityTests;

/// <summary>
/// The sentinel rows are written twice: by Liquibase into the real database, and by <c>HasData</c>
/// into the model, which is what seeds the databases this suite builds. Two spellings of one fact
/// drift, so these hold them together — and hold both to the rule that a sentinel is empty.
/// </summary>
public partial class NoRecordTests
{
    private static readonly string TableSqlDirectory = Path.Combine(AppContext.BaseDirectory, "DbTables");

    [Fact]
    public void SentinelPerson_IsEmptyThroughout()
    {
        var person = NoRecord.Person();

        person.PersonId.Should().Be(Guid.Empty);
        person.Name.Should().BeEmpty();
        person.Email.Should().BeEmpty();
    }

    [Fact]
    public void SentinelHat_IsEmptyThroughout()
    {
        var hat = NoRecord.Hat();

        hat.HatId.Should().Be(Guid.Empty);
        // Self-consistent: following the sentinel hat's organizer reaches the sentinel person
        // rather than nothing at all.
        hat.OrganizerPersonId.Should().Be(Guid.Empty);
        hat.Name.Should().BeEmpty();
        hat.NameNormalized.Should().BeEmpty();
        hat.Status.Should().BeEmpty();
        hat.AdditionalInformation.Should().BeEmpty();
        hat.PriceRange.Should().BeEmpty();
        hat.InvitationsQueuedAt.Should().Be(DateTimeOffset.MinValue);
        hat.InvitationsSentFromIp.Should().BeEmpty();
        hat.CreatedAt.Should().Be(DateTimeOffset.MinValue);
    }

    /// <summary>
    /// A fresh instance every time, so a caller that mutates what it is handed cannot leave a
    /// sentinel that is no longer empty behind for the next one.
    /// </summary>
    [Fact]
    public void SentinelFactories_HandOutDistinctInstances()
    {
        NoRecord.Person().Should().NotBeSameAs(NoRecord.Person());
        NoRecord.Hat().Should().NotBeSameAs(NoRecord.Hat());
        NoRecord.Participant().Should().NotBeSameAs(NoRecord.Participant());
    }

    [Fact]
    public void SentinelParticipant_IsEmptyThroughout()
    {
        var participant = NoRecord.Participant();

        participant.ParticipantId.Should().Be(Guid.Empty);
        participant.HatId.Should().Be(Guid.Empty);
        participant.PersonId.Should().Be(Guid.Empty);
        // It draws itself, which is what makes following any unshaken pick an inner join.
        participant.PickedRecipientParticipantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void EverySentinelValueInTheMigrationSql_IsEmpty()
    {
        foreach (var file in SeedFiles)
        {
            var values = ParseInsertedValues(file);

            values.Should().NotBeEmpty($"{file} should insert a sentinel row");

            values
                .Where(value => value != "''" && value != AllZeroId && !IsMinimumTimestamp(value))
                .Should().BeEmpty($"every column {file} inserts should be empty, the all-zero id, or the minimum timestamp");
        }
    }

    /// <summary>
    /// The seed SQL and the model have to name the same columns. A column added to the table but
    /// left out of the seed would leave the sentinel row in the real database describing something
    /// narrower than the one the suite tests against — and, since the table has no defaults, the
    /// INSERT would simply fail on deploy.
    /// </summary>
    [Theory]
    [InlineData("person--0002.sql", "person")]
    [InlineData("hat--0002.sql", "hat")]
    [InlineData("participant--0002.sql", "participant")]
    public void TheMigrationSeed_NamesEveryColumnTheModelMaps(string file, string table)
    {
        var entity = BuildModel()
            .GetEntityTypes()
            .Single(candidate => candidate.GetTableName() == table);

        var storeObject = StoreObjectIdentifier.Table(table, entity.GetSchema());

        var mapped = entity.GetProperties()
            .Select(property => property.GetColumnName(storeObject))
            .Where(column => column is not null)
            .ToImmutableSortedSet()!;

        var seeded = ParseInsertedColumns(file);

        seeded.Should().BeEquivalentTo(mapped);

        // Every named column gets a value, which is what makes the row insertable into a table with
        // no defaults to fall back on.
        ParseInsertedValues(file).Should().HaveCount(seeded.Count);
    }

    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<GiftExchangeDbContext>()
            .UseNpgsql("Host=localhost;Database=placeholder")
            .Options;

        using var context = new GiftExchangeDbContext(options);
        return context.Model;
    }

    private static readonly ImmutableList<string> SeedFiles =
        ["person--0002.sql", "hat--0002.sql", "participant--0002.sql"];

    private const string AllZeroId = "'00000000-0000-0000-0000-000000000000'";

    private static bool IsMinimumTimestamp(string value) =>
        value.StartsWith("'0001-01-01", StringComparison.Ordinal);

    /// <summary>The column names from the INSERT list of a seed file.</summary>
    private static ImmutableSortedSet<string> ParseInsertedColumns(string file)
    {
        var columns = ColumnList().Match(ReadSql(file));

        columns.Success.Should().BeTrue($"{file} should name the columns it inserts");

        return columns.Groups[1].Value
            .Split(',')
            .Select(column => column.Trim())
            .Where(column => column.Length > 0)
            .ToImmutableSortedSet();
    }

    /// <summary>The quoted values from the VALUES clause of a seed file.</summary>
    private static ImmutableList<string> ParseInsertedValues(string file)
    {
        var values = ValuesClause().Match(ReadSql(file));

        values.Success.Should().BeTrue($"{file} should hold one INSERT with a VALUES clause");

        return values.Groups[1].Value
            .Split(',')
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToImmutableList();
    }

    private static string ReadSql(string file) =>
        Regex.Replace(File.ReadAllText(Path.Combine(TableSqlDirectory, file)), "--.*", string.Empty);

    /// <summary>The parenthesised group between the table name and the VALUES keyword.</summary>
    [GeneratedRegex(@"INSERT\s+INTO\s+\w+\s*\(([^)]*)\)\s*VALUES", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ColumnList();

    /// <summary>
    /// The parenthesised group following the VALUES keyword. Anchoring on the keyword is what keeps
    /// the column list, which is parenthesised too, out of the match.
    /// </summary>
    [GeneratedRegex(@"VALUES\s*\(([^)]*)\)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex ValuesClause();
}
