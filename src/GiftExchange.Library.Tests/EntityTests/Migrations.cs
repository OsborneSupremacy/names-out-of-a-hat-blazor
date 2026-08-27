using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GiftExchange.Library.Tests.EntityTests;

/// <summary>
/// The migrations as Liquibase will apply them: every changeset's SQL, in changelog order.
///
/// The changelog is the subject rather than db/tables, because only part of the schema lives in
/// those files. Statements small enough to read at a glance sit inline in the changelog instead, so
/// a test that walked the directory would quietly stop seeing half the schema — and the drift
/// checks built on it would keep passing while covering less.
/// </summary>
internal static partial class Migrations
{
    private static readonly string DbDirectory = Path.Combine(AppContext.BaseDirectory, "Db");

    /// <summary>One statement, with the changeset it came from for use in assertion messages.</summary>
    internal sealed record Statement(string ChangeSetId, string Sql);

    /// <summary>
    /// In application order. Comments are stripped, so callers match against SQL rather than prose
    /// that happens to mention a table.
    /// </summary>
    internal static ImmutableList<Statement> Statements { get; } = Load();

    private static ImmutableList<Statement> Load()
    {
        var changeLog = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            // author, comment, rollback and the sqlFile flags are not the subject here.
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<ChangeLog>(File.ReadAllText(Path.Combine(DbDirectory, "changelog.yaml")));

        return changeLog.DatabaseChangeLog
            .Select(entry => entry.ChangeSet)
            .Where(changeSet => changeSet is not null)
            .SelectMany(changeSet => changeSet!.Changes.Select(change => (changeSet, change)))
            .Select(pair => new Statement(pair.changeSet.Id, Read(pair.change)))
            .Where(statement => statement.Sql.Length > 0)
            .ToImmutableList();
    }

    private static string Read(Change change)
    {
        if (change.Sql is not null)
            return Strip(change.Sql.Sql);

        if (change.SqlFile is null)
            return string.Empty;

        // Grants and role mappings are not table DDL, and the test project does not copy them.
        if (change.SqlFile.Path.StartsWith("roles/", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var path = Path.Combine(DbDirectory, change.SqlFile.Path.Replace('/', Path.DirectorySeparatorChar));

        // Not tolerated: a changelog naming a file that is not there would otherwise read as a
        // schema with one fewer statement in it.
        File.Exists(path).Should().BeTrue($"the changelog references {change.SqlFile.Path}");

        return Strip(File.ReadAllText(path));
    }

    private static string Strip(string sql) => LineComment().Replace(sql, string.Empty);

    [GeneratedRegex("--.*")]
    private static partial Regex LineComment();

    private sealed class ChangeLog
    {
        public List<ChangeSetEntry> DatabaseChangeLog { get; init; } = [];
    }

    private sealed class ChangeSetEntry
    {
        public ChangeSet? ChangeSet { get; init; }
    }

    private sealed class ChangeSet
    {
        public string Id { get; init; } = string.Empty;

        public List<Change> Changes { get; init; } = [];
    }

    private sealed class Change
    {
        public InlineSql? Sql { get; init; }

        public SqlFileReference? SqlFile { get; init; }
    }

    private sealed class InlineSql
    {
        public string Sql { get; init; } = string.Empty;
    }

    private sealed class SqlFileReference
    {
        public string Path { get; init; } = string.Empty;
    }
}
