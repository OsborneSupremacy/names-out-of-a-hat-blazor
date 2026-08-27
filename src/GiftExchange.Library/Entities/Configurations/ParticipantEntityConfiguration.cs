namespace GiftExchange.Library.Entities.Configurations;

internal class ParticipantEntityConfiguration : IEntityTypeConfiguration<ParticipantEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantEntity> builder)
    {
        builder.ToTable("participant");

        builder.HasKey(participant => participant.ParticipantId);
        builder.Property(participant => participant.ParticipantId).HasColumnName("participant_id").ValueGeneratedNever();

        builder.Property(participant => participant.HatId).HasColumnName("hat_id").IsRequired();
        builder.Property(participant => participant.PersonId).HasColumnName("person_id").IsRequired();

        // Mapped as a plain column with no relationship behind it. See the remarks on the property:
        // a navigation would make EF emit a foreign key, and the all-zero sentinel that stands for
        // "not drawn yet" would fail it.
        builder
            .Property(participant => participant.PickedRecipientParticipantId)
            .HasColumnName("picked_recipient_participant_id")
            .IsRequired();

        // One row per person per hat. Nothing here constrains display names — those live on person,
        // and it is AddParticipantService that refuses a name already taken within the hat.
        builder
            .HasIndex(participant => new { participant.HatId, participant.PersonId })
            .HasDatabaseName("uq_participant_hat_person")
            .IsUnique();

        builder
            .HasOne(participant => participant.Hat)
            .WithMany(hat => hat.Participants)
            .HasForeignKey(participant => participant.HatId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne(participant => participant.Person)
            .WithMany(person => person.Participations)
            .HasForeignKey(participant => participant.PersonId)
            .OnDelete(DeleteBehavior.NoAction);

        // The row meaning "not taking part". It belongs to the sentinel hat and the sentinel
        // person, both of which EF seeds first because those two ends are real relationships.
        builder.HasData(NoRecord.Participant());
    }
}
