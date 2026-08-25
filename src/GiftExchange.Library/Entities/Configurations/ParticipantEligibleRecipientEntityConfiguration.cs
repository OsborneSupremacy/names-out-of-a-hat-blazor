namespace GiftExchange.Library.Entities.Configurations;

internal class ParticipantEligibleRecipientEntityConfiguration
    : IEntityTypeConfiguration<ParticipantEligibleRecipientEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantEligibleRecipientEntity> builder)
    {
        builder.ToTable("participant_eligible_recipients");

        builder.HasKey(row => row.ParticipantEligibleRecipientsId);

        builder
            .Property(row => row.ParticipantEligibleRecipientsId)
            .HasColumnName("participant_eligible_recipients_id")
            .ValueGeneratedNever();

        builder.Property(row => row.ParticipantId).HasColumnName("participant_id").IsRequired();
        builder.Property(row => row.EligibleParticipantId).HasColumnName("eligible_participant_id").IsRequired();

        // Restores what the old composite primary key guaranteed: a participant cannot be made
        // eligible for the same recipient twice. Duplicates would inflate the eligibility counts
        // EligibilityValidationService reads.
        builder
            .HasIndex(row => new { row.ParticipantId, row.EligibleParticipantId })
            .HasDatabaseName("uq_eligible_recipients_participant_eligible")
            .IsUnique();

        builder
            .HasIndex(row => row.EligibleParticipantId)
            .HasDatabaseName("idx_eligible_recipients_eligible_participant");

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
