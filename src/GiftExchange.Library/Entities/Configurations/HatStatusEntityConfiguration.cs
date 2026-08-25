namespace GiftExchange.Library.Entities.Configurations;

internal class HatStatusEntityConfiguration : IEntityTypeConfiguration<HatStatusEntity>
{
    public void Configure(EntityTypeBuilder<HatStatusEntity> builder)
    {
        builder.ToTable("hat_status");

        builder.HasKey(status => status.Status);

        builder
            .Property(status => status.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .ValueGeneratedNever();
    }
}
