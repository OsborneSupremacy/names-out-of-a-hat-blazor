using Amazon.AuroraDsql.Npgsql;

namespace GiftExchange.Library.Providers;

/// <summary>
/// Builds the pooled data source the application connects through. The AWS connector owns IAM
/// token generation and refresh, and enforces TLS with verify-full, so none of that is handled
/// here.
/// </summary>
internal static class DsqlDataSourceProvider
{
    /// <summary>
    /// The database role the Lambda connects as. It holds CRUD on the tables and nothing more;
    /// admin is reserved for migrations. Anything other than "admin" makes the connector request
    /// a regular auth token rather than an admin one.
    /// </summary>
    private const string DatabaseUser = "giftexchange_user";

    private const int OccMaxRetries = 3;

    /// <summary>
    /// Blocks once, on the first resolution in a cold container, because the connector's factory
    /// is async and the service provider is built synchronously. There is no synchronization
    /// context in Lambda, so this cannot deadlock.
    /// </summary>
    public static DsqlDataSource Create() =>
        AuroraDsql
            .CreateDataSourceAsync(new DsqlConfig
            {
                Host = EnvReader.GetStringValue("DSQL_ENDPOINT"),
                // Set explicitly rather than left to the connector's hostname parsing, which
                // only works for a *.dsql.<region>.on.aws endpoint.
                Region = EnvReader.GetStringValue("AWS_REGION"),
                User = DatabaseUser,
                Database = "postgres",
                OccMaxRetries = OccMaxRetries
            })
            .GetAwaiter()
            .GetResult();
}
