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
}
