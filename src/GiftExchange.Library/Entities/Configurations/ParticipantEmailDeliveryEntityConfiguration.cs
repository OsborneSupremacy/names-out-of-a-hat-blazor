namespace GiftExchange.Library.Entities.Configurations;

internal class ParticipantEmailDeliveryEntityConfiguration : IEntityTypeConfiguration<ParticipantEmailDeliveryEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantEmailDeliveryEntity> builder)
    {
        builder.ToTable("participant_email_delivery");

        builder.HasKey(delivery => delivery.ParticipantEmailDeliveryId);
        builder
            .Property(delivery => delivery.ParticipantEmailDeliveryId)
            .HasColumnName("participant_email_delivery_id")
            .ValueGeneratedNever();

        // No relationship behind it, for the reason given on the property.
        builder.Property(delivery => delivery.ParticipantId).HasColumnName("participant_id").IsRequired();

        builder
            .Property(delivery => delivery.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(20)
            .IsRequired();

        builder
            .Property(delivery => delivery.SesMessageId)
            .HasColumnName("ses_message_id")
            .HasMaxLength(200)
            .IsRequired();

        builder
            .Property(delivery => delivery.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder
            .Property(delivery => delivery.Detail)
            .HasColumnName("detail")
            .HasMaxLength(DetailMaxLength)
            .IsRequired();

        builder.Property(delivery => delivery.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(delivery => delivery.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // An event knows the message id and nothing else it could find a row by, so this is the
        // whole route from an event to the row it updates.
        //
        // Unique, like uq_gift_idea_token_hash: the table starts empty, so there is no crowd of
        // backfilled rows for a unique index to reject.
        builder
            .HasIndex(delivery => delivery.SesMessageId)
            .HasDatabaseName("uq_participant_email_delivery_message")
            .IsUnique();

        // Not unique: several messages per participant is the intended state, and more will
        // accumulate every time an exchange sends something. Ordered by occurred_at because the
        // read wants the newest and "newest" is decided by what SES said rather than by the id.
        builder
            .HasIndex(delivery => new { delivery.ParticipantId, delivery.OccurredAt })
            .HasDatabaseName("idx_participant_email_delivery_participant");
    }

    /// <summary>
    /// What the column holds, and therefore where the service truncates. Stated once, here, so the
    /// two cannot drift and hand DSQL a string it will refuse.
    /// </summary>
    internal const int DetailMaxLength = 500;
}
