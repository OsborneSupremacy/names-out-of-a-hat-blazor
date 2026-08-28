namespace GiftExchange.Library.Entities.Configurations;

internal class ContributedGiftIdeaEntityConfiguration : IEntityTypeConfiguration<ContributedGiftIdeaEntity>
{
    public void Configure(EntityTypeBuilder<ContributedGiftIdeaEntity> builder)
    {
        builder.ToTable("contributed_gift_idea");

        builder.HasKey(contribution => contribution.ContributedGiftIdeaId);

        builder
            .Property(contribution => contribution.ContributedGiftIdeaId)
            .HasColumnName("contributed_gift_idea_id")
            .ValueGeneratedNever();

        // No relationship behind it, for the reason given on GiftIdeaEntity.ParticipantId.
        builder.Property(contribution => contribution.GiftIdeaAskId).HasColumnName("gift_idea_ask_id").IsRequired();

        builder
            .Property(contribution => contribution.Ideas)
            .HasColumnName("ideas")
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(contribution => contribution.CreatedAt).HasColumnName("created_at").IsRequired();

        builder
            .Property(contribution => contribution.InboundMessageId)
            .HasColumnName("inbound_message_id")
            .HasMaxLength(255)
            .IsRequired();

        // Both questions this table is asked start from an ask. Not unique — accumulating rows is
        // the point, as it is in gift_idea.
        builder
            .HasIndex(contribution => new { contribution.GiftIdeaAskId, contribution.CreatedAt })
            .HasDatabaseName("idx_contributed_gift_idea_ask");
    }
}
