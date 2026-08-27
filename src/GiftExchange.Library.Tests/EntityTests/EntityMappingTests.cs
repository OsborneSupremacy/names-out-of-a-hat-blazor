using System.Text.RegularExpressions;
using GiftExchange.Library.Contexts;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GiftExchange.Library.Tests.EntityTests;

/// <summary>
/// Liquibase owns the schema and EF owns the mapping, and nothing connects the two. Left alone
/// they drift, and the first symptom is a runtime error against a real cluster. These tests build
/// the model offline and compare it to the migration SQL.
/// </summary>
public class EntityMappingTests
{
    private static readonly string TableSqlDirectory = Path.Combine(AppContext.BaseDirectory, "DbTables");

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

        var created = ParseTableColumns().Keys.ToImmutableSortedSet();

        mapped.Should().BeSubsetOf(created, "every entity should map to a table db/tables creates");
    }

    [Fact]
    public void EveryMappedColumn_ExistsInTheMigrationSql()
    {
        var created = ParseTableColumns();
        var missing = new List<string>();

        foreach (var entity in BuildModel().GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is null || !created.TryGetValue(tableName, out var columns)) continue;

            var storeObject = StoreObjectIdentifier.Table(tableName, entity.GetSchema());

            foreach (var property in entity.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);

                if (columnName is not null && !columns.Contains(columnName))
                    missing.Add($"{entity.ClrType.Name}.{property.Name} maps to {tableName}.{columnName}, which the migrations do not create");
            }
        }

        missing.Should().BeEmpty();
    }

    /// <summary>
    /// Replays the migration SQL far enough to know which columns each table ends up with.
    ///
    /// Files are applied in filename order, which the object--nnnn convention makes chronological,
    /// because a column can be introduced by a later ALTER rather than the original CREATE.
    /// </summary>
    private static Dictionary<string, ImmutableHashSet<string>> ParseTableColumns()
    {
        var tables = new Dictionary<string, ImmutableHashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(TableSqlDirectory, "*.sql").Order(StringComparer.Ordinal))
        {
            var sql = Regex.Replace(File.ReadAllText(file), "--.*", string.Empty);

            var create = Regex.Match(sql, @"CREATE\s+TABLE\s+(\w+)\s*\((.*)\)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (create.Success)
            {
                tables[create.Groups[1].Value] = ParseColumnNames(create.Groups[2].Value);
                continue;
            }

            var addColumn = Regex.Match(sql, @"ALTER\s+TABLE\s+(\w+)\s+ADD\s+COLUMN\s+(\w+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (addColumn.Success && tables.TryGetValue(addColumn.Groups[1].Value, out var existing))
            {
                tables[addColumn.Groups[1].Value] = existing.Add(addColumn.Groups[2].Value);
                continue;
            }

            var dropColumn = Regex.Match(sql, @"ALTER\s+TABLE\s+(\w+)\s+DROP\s+COLUMN\s+(\w+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (dropColumn.Success && tables.TryGetValue(dropColumn.Groups[1].Value, out var remaining))
                tables[dropColumn.Groups[1].Value] = remaining.Remove(dropColumn.Groups[2].Value);
        }

        return tables;
    }

    private static ImmutableHashSet<string> ParseColumnNames(string tableBody) =>
        tableBody
            .Split(',')
            .Select(definition => definition.Trim())
            .Where(definition => definition.Length > 0)
            .Select(definition => definition.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)[0])
            // Table-level constraints rather than columns.
            .Where(token => !IsConstraintKeyword(token))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    private static bool IsConstraintKeyword(string token) =>
        token.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase)
        || token.Equals("UNIQUE", StringComparison.OrdinalIgnoreCase)
        || token.Equals("FOREIGN", StringComparison.OrdinalIgnoreCase)
        || token.Equals("CHECK", StringComparison.OrdinalIgnoreCase)
        || token.Equals("CONSTRAINT", StringComparison.OrdinalIgnoreCase);
}
