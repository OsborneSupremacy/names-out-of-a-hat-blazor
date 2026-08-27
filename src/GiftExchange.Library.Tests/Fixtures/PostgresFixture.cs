using GiftExchange.Library.Contexts;
using GiftExchange.Library.Interceptors;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;

namespace GiftExchange.Library.Tests.Fixtures;

/// <summary>
/// Stands in for Aurora DSQL, which has no local emulator.
///
/// Postgres is the closest thing that runs offline: same wire protocol and SQL dialect, so the
/// EF mapping and the queries are genuinely exercised. It is not DSQL, though — it has foreign
/// keys, sequences and no optimistic concurrency control, so it will happily accept things DSQL
/// would reject and never reproduce a write conflict. Anything depending on those needs a real
/// cluster.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:17-alpine";

    private readonly PostgreSqlContainer _container;

    public PostgresFixture()
    {
        DotEnv.Load();
        _container = new PostgreSqlBuilder(PostgresImage).Build();
    }

    public IDbContextFactory<GiftExchangeDbContext> CreateContextFactory(params IInterceptor[] extraInterceptors) =>
        new PostgresContextFactory(_container.GetConnectionString(), extraInterceptors);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // EnsureCreated builds the whole schema from the EF model. There is no reference data to
        // seed alongside it: hat.status is a plain string column, checked against
        // GiftExchange.Library.Models.HatStatuses by the application rather than by a table.
        await using var context = CreateContextFactory().CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private sealed class PostgresContextFactory(string connectionString, IInterceptor[] extraInterceptors)
        : IDbContextFactory<GiftExchangeDbContext>
    {
        public GiftExchangeDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<GiftExchangeDbContext>()
                .UseNpgsql(connectionString)
                // Registered here for the same reason as in ServiceProviderBuilder: without it
                // these tests would exercise a differently configured context than the one that
                // ships, and the isolation level assertions would prove nothing.
                .AddInterceptors(new RepeatableReadTransactionInterceptor())
                .AddInterceptors(extraInterceptors)
                .Options);
    }
}

/// <summary>
/// One container for the whole run rather than one per test class.
/// </summary>
[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
