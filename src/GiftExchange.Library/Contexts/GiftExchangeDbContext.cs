namespace GiftExchange.Library.Contexts;

public class GiftExchangeDbContext : DbContext
{
    public GiftExchangeDbContext(DbContextOptions<GiftExchangeDbContext> options) : base(options)
    {
        // DSQL rejects SAVEPOINT outright ("0A000: unsupported transaction statement"). EF creates
        // one whenever SaveChanges runs inside a transaction that was already open, so it can undo
        // just that save without discarding the surrounding transaction.
        //
        // Losing that costs nothing here: every transaction this application opens goes through
        // GiftExchangeProvider.InTransactionAsync, which abandons the whole transaction on failure
        // rather than trying to continue after a partial one.
        //
        // Postgres supports savepoints, so nothing but a real cluster reveals this.
        Database.AutoSavepointsEnabled = false;
    }

    public DbSet<HatEntity> Hats => Set<HatEntity>();

    public DbSet<HatStatusEntity> HatStatuses => Set<HatStatusEntity>();

    public DbSet<ParticipantEntity> Participants => Set<ParticipantEntity>();

    public DbSet<ParticipantEligibleRecipientEntity> ParticipantEligibleRecipients =>
        Set<ParticipantEligibleRecipientEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GiftExchangeDbContext).Assembly);
}
