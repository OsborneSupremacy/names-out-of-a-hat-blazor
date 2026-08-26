using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GiftExchange.Library.Interceptors;

/// <summary>
/// Forces every transaction to REPEATABLE READ, which is the only isolation level Aurora DSQL
/// accepts. Npgsql asks for READ COMMITTED by default and DSQL rejects it with
/// "0A000: Unsupported isolation level".
///
/// This has to be an interceptor rather than an argument at the call sites, because EF opens
/// transactions we never ask for: SaveChanges wraps a multi-statement batch in one of its own, and
/// there is no overload to influence its isolation level.
///
/// Postgres accepts READ COMMITTED, so a test suite running against Postgres cannot notice when
/// this is missing. GiftExchangeProviderTests asserts the level explicitly for that reason.
/// </summary>
[UsedImplicitly]
internal class RepeatableReadTransactionInterceptor : DbTransactionInterceptor
{
    public override InterceptionResult<DbTransaction> TransactionStarting(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result
    ) =>
        eventData.IsolationLevel == IsolationLevel.RepeatableRead
            ? result
            : InterceptionResult<DbTransaction>.SuppressWithResult(
                connection.BeginTransaction(IsolationLevel.RepeatableRead));

    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection,
        TransactionStartingEventData eventData,
        InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.IsolationLevel == IsolationLevel.RepeatableRead)
            return result;

        var transaction = await connection
            .BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken)
            .ConfigureAwait(false);

        return InterceptionResult<DbTransaction>.SuppressWithResult(transaction);
    }
}
