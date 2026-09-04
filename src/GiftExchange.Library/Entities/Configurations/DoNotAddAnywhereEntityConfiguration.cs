namespace GiftExchange.Library.Entities.Configurations;

internal class DoNotAddAnywhereEntityConfiguration : IEntityTypeConfiguration<DoNotAddAnywhereEntity>
{
    public void Configure(EntityTypeBuilder<DoNotAddAnywhereEntity> builder)
    {
        builder.ToTable("do_not_add_anywhere");

        builder.HasKey(block => block.DoNotAddAnywhereId);
        builder
            .Property(block => block.DoNotAddAnywhereId)
            .HasColumnName("do_not_add_anywhere_id")
            .ValueGeneratedNever();

        builder
            .Property(block => block.EmailNormalized)
            .HasColumnName("email_normalized")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(block => block.CreatedAt).HasColumnName("created_at").IsRequired();

        // One row per address. The whole table is this index.
        builder
            .HasIndex(block => block.EmailNormalized)
            .HasDatabaseName("uq_do_not_add_anywhere")
            .IsUnique();
    }
}
