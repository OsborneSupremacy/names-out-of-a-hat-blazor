using System.Text.RegularExpressions;
using GiftExchange.Library.Contexts;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GiftExchange.Library.Tests.EntityTests;

/// <summary>
/// Liquibase owns the schema and EF owns the mapping, and nothing connects the two. Left alone
/// they drift, and the first symptom is a runtime error against a real cluster. These tests build
/// the model offline and compare it to the migration SQL.
///
/// They also hold the schema to the rules it is written under: no nullable column, no default, and
/// a primary key named for its own table rather than a bare "id".
/// </summary>
public partial class EntityMappingTests
{
    /// <summary>
    /// Builds the model without opening a connection. Any misconfigured relationship or duplicate
    /// mapping throws here.
    /// </summary>
    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<GiftExchangeDbContext>()
            .UseNpgsql("Host=localhost;Database=placeholder")
            .Options;

        using var context = new GiftExchangeDbContext(options);
        return context.Model;
    }

    [Fact]
    public void Model_BuildsWithoutError()
    {
        var build = BuildModel;

        build.Should().NotThrow();
    }

    [Fact]
    public void EveryEntity_MapsToATableTheMigrationsCreate()
    {
        var mapped = BuildModel()
            .GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(name => name is not null)
            .ToImmutableSortedSet()!;

        var surviving = ParseTables().Keys.ToImmutableSortedSet();

        mapped.Should().BeSubsetOf(surviving, "every entity should map to a table the migrations leave in place");
    }

    [Fact]
    public void EveryMappedColumn_ExistsInTheMigrationSql()
    {
        var tables = ParseTables();
        var missing = new List<string>();

        foreach (var entity in BuildModel().GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is null || !tables.TryGetValue(tableName, out var columns)) continue;

            var storeObject = StoreObjectIdentifier.Table(tableName, entity.GetSchema());

            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);

                if (columnName is not null && !columns.ContainsKey(columnName))
                    missing.Add($"{entity.ClrType.Name}.{property.Name} maps to {tableName}.{columnName}, which the migrations do not create");
            }
        }

        missing.Should().BeEmpty();
    }

    /// <summary>
    /// The same rule on the other side of the mapping. The SQL and the model are written
    /// independently, so a column that is NOT NULL in one and optional in the other would only
    /// show up against a real cluster — as a write EF was willing to send and DSQL refused.
    /// </summary>
    [Fact]
    public void NoMappedProperty_IsNullable()
    {
        var nullable =
            from entity in BuildModel().GetEntityTypes()
            from property in entity.GetProperties()
            where property.IsNullable
            select $"{entity.ClrType.Name}.{property.Name}";

        nullable.Should().BeEmpty();
    }

    /// <summary>
    /// The DDL EF would emit for this model carries no DEFAULT and no nullable column either.
    ///
    /// This reads the generated script rather than the property annotations because
    /// <c>GetDefaultValue()</c> is not the question it looks like: EF answers it with the CLR
    /// default for every property, configured or not. The script is what the model actually means.
    /// </summary>
    [Fact]
    public void TheDdlTheModelWouldEmit_HasNoDefaultsAndNoNullableColumns()
    {
        var options = new DbContextOptionsBuilder<GiftExchangeDbContext>()
            .UseNpgsql("Host=localhost;Database=placeholder")
            .Options;

        using var context = new GiftExchangeDbContext(options);

        var script = context.Database.GenerateCreateScript();

        script.Should().NotContain("DEFAULT", "no column should be given a default");

        var columns = script
            .Split('\n')
            .Select(line => line.Trim().TrimEnd(','))
            .Where(line => ColumnDefinition().IsMatch(line))
            .ToList();

        // Guards the guard: a pattern that matched nothing would let the assertion below pass
        // without having looked at a single column.
        columns.Should().HaveCountGreaterThan(BuildModel().GetEntityTypes().Count());

        columns
            .Where(line => !line.Contains("NOT NULL", StringComparison.Ordinal))
            .Should().BeEmpty("every column the model describes should be NOT NULL");
    }

    /// <summary>
    /// Columns the database is unable to declare NOT NULL, and the application therefore keeps
    /// filled on its own.
    ///
    /// DSQL rejects ALTER COLUMN ... SET NOT NULL, and will not add a column with a default, so a
    /// column introduced after its table already held rows can never be tightened. Listing one here
    /// is a deliberate statement that the application owns the invariant — the same arrangement as
    /// the foreign keys DSQL cannot enforce either — not an oversight.
    ///
    /// Only a column added by a later ALTER can qualify. A column present in a CREATE TABLE has no
    /// excuse, and none belongs here.
    /// </summary>
    private static readonly ImmutableHashSet<string> ColumnsDsqlCannotConstrain =
    [
        // Added by hat--0003. HatEntity.CopiedFromHatId is non-nullable, which is what actually
        // keeps it filled.
        "hat.copied_from_hat_id"
    ];

    /// <summary>
    /// Nothing is nullable. Absence is spelled with a value instead — the all-zero UUID, the
    /// minimum timestamp, the empty string — so reading a row never means asking whether a column
    /// is there.
    /// </summary>
    [Fact]
    public void NoSurvivingColumn_IsNullable()
    {
        var nullable =
            from table in ParseTables()
            from column in table.Value
            where !column.Value.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase)
                  && !column.Value.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
            select $"{table.Key}.{column.Key}";

        nullable.Should().BeSubsetOf(ColumnsDsqlCannotConstrain);
    }

    /// <summary>
    /// An entry that names a column the migrations do declare NOT NULL is stale, and would quietly
    /// excuse the next column to take its place.
    /// </summary>
    [Fact]
    public void EveryDocumentedNullableColumn_IsStillNullable()
    {
        var nullable =
            from table in ParseTables()
            from column in table.Value
            where !column.Value.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase)
                  && !column.Value.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
            select $"{table.Key}.{column.Key}";

        ColumnsDsqlCannotConstrain.Should().BeSubsetOf(nullable);
    }

    /// <summary>
    /// Nothing has a default. Every field is stated by whoever writes the row, so what the
    /// application believes it wrote is what is there.
    /// </summary>
    [Fact]
    public void NoSurvivingColumn_HasADefault()
    {
        var defaulted =
            from table in ParseTables()
            from column in table.Value
            where column.Value.Contains("DEFAULT", StringComparison.OrdinalIgnoreCase)
            select $"{table.Key}.{column.Key}";

        defaulted.Should().BeEmpty();
    }

    /// <summary>
    /// A primary key is named for the table it identifies, never a bare "id", so a column keeps
    /// its meaning once it has been carried into another table as a foreign key.
    /// </summary>
    [Fact]
    public void EveryPrimaryKey_IsNamedForItsTable()
    {
        var misnamed =
            from table in ParseTables()
            from column in table.Value
            where column.Value.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
                  && column.Key != $"{table.Key}_id"
            select $"{table.Key}.{column.Key} should be {table.Key}_id";

        misnamed.Should().BeEmpty();
    }

    /// <summary>
    /// Replays the migrations far enough to know which tables survive and which columns each of
    /// them ends up with, keyed by column name and holding the full definition so the rules above
    /// have something to read.
    ///
    /// Applied in changelog order, which is the order Liquibase applies them, because a column can
    /// be introduced by a later ALTER rather than the original CREATE and a table can be dropped
    /// entirely further down.
    /// </summary>
    private static Dictionary<string, ImmutableDictionary<string, string>> ParseTables()
    {
        var tables = new Dictionary<string, ImmutableDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, sql) in Migrations.Statements)
        {
            var create = Regex.Match(sql, @"CREATE\s+TABLE\s+(\w+)\s*\((.*)\)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (create.Success)
            {
                tables[create.Groups[1].Value] = ParseColumns(create.Groups[2].Value);
                continue;
            }

            var dropTable = Regex.Match(sql, @"DROP\s+TABLE\s+(\w+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (dropTable.Success)
            {
                tables.Remove(dropTable.Groups[1].Value);
                continue;
            }

            var addColumn = Regex.Match(sql, @"ALTER\s+TABLE\s+(\w+)\s+ADD\s+COLUMN\s+(\w+)([^;]*)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (addColumn.Success && tables.TryGetValue(addColumn.Groups[1].Value, out var existing))
            {
                tables[addColumn.Groups[1].Value] = existing.SetItem(
                    addColumn.Groups[2].Value,
                    addColumn.Groups[3].Value.Trim());
                continue;
            }

            var dropColumn = Regex.Match(sql, @"ALTER\s+TABLE\s+(\w+)\s+DROP\s+COLUMN\s+(\w+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (dropColumn.Success && tables.TryGetValue(dropColumn.Groups[1].Value, out var remaining))
                tables[dropColumn.Groups[1].Value] = remaining.Remove(dropColumn.Groups[2].Value);
        }

        return tables;
    }

    private static ImmutableDictionary<string, string> ParseColumns(string tableBody) =>
        tableBody
            .Split(',')
            .Select(definition => definition.Trim())
            .Where(definition => definition.Length > 0)
            // Table-level constraints rather than columns.
            .Where(definition => !IsConstraintKeyword(FirstToken(definition)))
            .ToImmutableDictionary(FirstToken, definition => definition, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A column line in a generated create script: a lower-case identifier, then its type.
    /// CONSTRAINT and CREATE lines start with a capital, so they fall outside it.
    /// </summary>
    [GeneratedRegex(@"^[a-z_]+ \w")]
    private static partial Regex ColumnDefinition();

    private static string FirstToken(string definition) =>
        definition.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)[0];

    private static bool IsConstraintKeyword(string token) =>
        token.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase)
        || token.Equals("UNIQUE", StringComparison.OrdinalIgnoreCase)
        || token.Equals("FOREIGN", StringComparison.OrdinalIgnoreCase)
        || token.Equals("CHECK", StringComparison.OrdinalIgnoreCase)
        || token.Equals("CONSTRAINT", StringComparison.OrdinalIgnoreCase);
}
