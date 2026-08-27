namespace GiftExchange.Library.Entities.Configurations;

internal class GiftIdeaTokenEntityConfiguration : IEntityTypeConfiguration<GiftIdeaTokenEntity>
{
    public void Configure(EntityTypeBuilder<GiftIdeaTokenEntity> builder)
    {
        builder.ToTable("gift_idea_token");

        builder.HasKey(token => token.GiftIdeaTokenId);
        builder.Property(token => token.GiftIdeaTokenId).HasColumnName("gift_idea_token_id").ValueGeneratedNever();

        // No relationship behind it, for the reason given on GiftIdeaEntity.ParticipantId.
        builder.Property(token => token.ParticipantId).HasColumnName("participant_id").IsRequired();

        builder
            .Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(token => token.IssuedAt).HasColumnName("issued_at").IsRequired();

        // Inbound mail knows the token and nothing else, so this is the one path that reaches a
        // participant without a hat id.
        builder
            .HasIndex(token => token.TokenHash)
            .HasDatabaseName("uq_gift_idea_token_hash")
            .IsUnique();

        // One live token per participant. Reissuing replaces the row rather than leaving a second
        // address that still writes to the same participant.
        builder
            .HasIndex(token => token.ParticipantId)
            .HasDatabaseName("uq_gift_idea_token_participant")
            .IsUnique();
    }
}
