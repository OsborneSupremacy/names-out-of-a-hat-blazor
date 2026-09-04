namespace GiftExchange.Library.Entities.Configurations;

internal class ParticipantLeaveTokenEntityConfiguration : IEntityTypeConfiguration<ParticipantLeaveTokenEntity>
{
    public void Configure(EntityTypeBuilder<ParticipantLeaveTokenEntity> builder)
    {
        builder.ToTable("participant_leave_token");

        builder.HasKey(token => token.ParticipantLeaveTokenId);
        builder
            .Property(token => token.ParticipantLeaveTokenId)
            .HasColumnName("participant_leave_token_id")
            .ValueGeneratedNever();

        // No relationship behind it, for the reason given on GiftIdeaEntity.ParticipantId.
        builder.Property(token => token.ParticipantId).HasColumnName("participant_id").IsRequired();

        builder
            .Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(token => token.IssuedAt).HasColumnName("issued_at").IsRequired();

        // A leave link knows the token and nothing else, so this is the one path that reaches a
        // participant without a hat id — the same shape as uq_gift_idea_token_hash.
        builder
            .HasIndex(token => token.TokenHash)
            .HasDatabaseName("uq_participant_leave_token_hash")
            .IsUnique();

        // So that reissuing, and the cleanup in DeleteParticipantAsync and DeleteHatAsync, can find
        // a participant's token without a scan.
        builder
            .HasIndex(token => token.ParticipantId)
            .HasDatabaseName("idx_participant_leave_token_participant");
    }
}
