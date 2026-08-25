namespace GiftExchange.Library.Contexts;

public class GiftExchangeDbContext : DbContext
{
    public GiftExchangeDbContext(DbContextOptions<GiftExchangeDbContext> options) : base(options) { }

    public DbSet<HatEntity> Hats => Set<HatEntity>();

    public DbSet<HatStatusEntity> HatStatuses => Set<HatStatusEntity>();

    public DbSet<ParticipantEntity> Participants => Set<ParticipantEntity>();

    public DbSet<ParticipantEligibleRecipientEntity> ParticipantEligibleRecipients =>
        Set<ParticipantEligibleRecipientEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GiftExchangeDbContext).Assembly);
}
