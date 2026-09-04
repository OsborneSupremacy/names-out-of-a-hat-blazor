namespace GiftExchange.Library.Entities.Configurations;

internal class DoNotAddToExchangeEntityConfiguration : IEntityTypeConfiguration<DoNotAddToExchangeEntity>
{
    public void Configure(EntityTypeBuilder<DoNotAddToExchangeEntity> builder)
    {
        builder.ToTable("do_not_add_to_exchange");

        builder.HasKey(block => block.DoNotAddToExchangeId);
        builder
            .Property(block => block.DoNotAddToExchangeId)
            .HasColumnName("do_not_add_to_exchange_id")
            .ValueGeneratedNever();

        // No relationship behind it. The row has to survive the hat, so a relationship that told EF
        // these two belonged together would be describing the opposite of what this table is for.
        builder.Property(block => block.HatId).HasColumnName("hat_id").IsRequired();

        builder
            .Property(block => block.EmailNormalized)
            .HasColumnName("email_normalized")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(block => block.CreatedAt).HasColumnName("created_at").IsRequired();

        // The only lookup this table serves, and the constraint that makes recording a refusal
        // idempotent. The address leads on all three lists, so every check is the same shape.
        builder
            .HasIndex(block => new { block.EmailNormalized, block.HatId })
            .HasDatabaseName("uq_do_not_add_to_exchange")
            .IsUnique();
    }
}
