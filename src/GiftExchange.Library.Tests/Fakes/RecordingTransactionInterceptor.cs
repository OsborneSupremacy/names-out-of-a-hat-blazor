using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GiftExchange.Library.Tests.Fakes;

/// <summary>
/// Records the isolation level of every transaction EF actually opens, including the ones it
/// opens by itself around a multi-statement SaveChanges.
/// </summary>
internal sealed class RecordingTransactionInterceptor : DbTransactionInterceptor
{
    public List<IsolationLevel> StartedLevels { get; } = [];

    /// <summary>
    /// DSQL cannot create savepoints, so any attempt is a bug even though Postgres allows it.
    /// </summary>
    public int SavepointsCreated { get; private set; }

    public override DbTransaction TransactionStarted(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result
    )
    {
        StartedLevels.Add(result.IsolationLevel);
        return base.TransactionStarted(connection, eventData, result);
    }

    public override ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default
    )
    {
        StartedLevels.Add(result.IsolationLevel);
        return base.TransactionStartedAsync(connection, eventData, result, cancellationToken);
    }

    public override InterceptionResult CreatingSavepoint(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result
    )
    {
        SavepointsCreated++;
        return base.CreatingSavepoint(transaction, eventData, result);
    }

    public override ValueTask<InterceptionResult> CreatingSavepointAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default
    )
    {
        SavepointsCreated++;
        return base.CreatingSavepointAsync(transaction, eventData, result, cancellationToken);
    }
}
