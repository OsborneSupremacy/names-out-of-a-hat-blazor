namespace GiftExchange.Library.Entities.Configurations;

internal class ParticipantEligibleRecipientEntityConfiguration
    : IEntityTypeConfiguration<ParticipantEligibleRecipientEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantEligibleRecipientEntity> builder)
    {
        builder.ToTable("participant_eligible_recipient");

        builder.HasKey(row => row.ParticipantEligibleRecipientId);

        builder
            .Property(row => row.ParticipantEligibleRecipientId)
            .HasColumnName("participant_eligible_recipient_id")
            .ValueGeneratedNever();

        builder.Property(row => row.ParticipantId).HasColumnName("participant_id").IsRequired();
        builder.Property(row => row.EligibleParticipantId).HasColumnName("eligible_participant_id").IsRequired();

        // A participant cannot be made eligible for the same recipient twice. Duplicates would
        // inflate the eligibility counts EligibilityValidationService reads.
        builder
            .HasIndex(row => new { row.ParticipantId, row.EligibleParticipantId })
            .HasDatabaseName("uq_participant_eligible_recipient")
            .IsUnique();

        builder
            .HasIndex(row => row.EligibleParticipantId)
            .HasDatabaseName("idx_participant_eligible_recipient_eligible");

        // Two relationships to the same table, so both need their navigations stated explicitly
        // or EF cannot work out which foreign key belongs to which end.
        builder
            .HasOne(row => row.Participant)
            .WithMany(participant => participant.EligibleRecipients)
            .HasForeignKey(row => row.ParticipantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne(row => row.EligibleParticipant)
            .WithMany(participant => participant.EligibleFor)
            .HasForeignKey(row => row.EligibleParticipantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
