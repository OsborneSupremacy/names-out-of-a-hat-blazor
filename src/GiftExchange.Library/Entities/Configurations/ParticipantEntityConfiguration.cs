namespace GiftExchange.Library.Entities.Configurations;

internal class ParticipantEntityConfiguration : IEntityTypeConfiguration<ParticipantEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantEntity> builder)
    {
        builder.ToTable("participants");

        builder.HasKey(participant => participant.Id);
        builder.Property(participant => participant.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(participant => participant.HatId).HasColumnName("hat_id").IsRequired();
        builder.Property(participant => participant.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(participant => participant.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(participant => participant.PickedRecipientId).HasColumnName("picked_recipient_id");

        // Deliberately not unique on name: two participants in one hat may share a display name.
        builder
            .HasIndex(participant => new { participant.HatId, participant.Email })
            .HasDatabaseName("uq_participants_hat_email")
            .IsUnique();

        builder
            .HasOne(participant => participant.Hat)
            .WithMany(hat => hat.Participants)
            .HasForeignKey(participant => participant.HatId)
            .OnDelete(DeleteBehavior.NoAction);

        // Self reference. DSQL cannot cascade, so removing a participant means clearing anyone
        // who drew them explicitly.
        builder
            .HasOne(participant => participant.PickedRecipient)
            .WithMany()
            .HasForeignKey(participant => participant.PickedRecipientId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
