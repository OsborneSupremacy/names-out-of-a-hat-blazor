namespace GiftExchange.Library.Entities.Configurations;

internal class PersonEntityConfiguration : IEntityTypeConfiguration<PersonEntity>
{
    public void Configure(EntityTypeBuilder<PersonEntity> builder)
    {
        builder.ToTable("person");

        builder.HasKey(person => person.PersonId);
        builder.Property(person => person.PersonId).HasColumnName("person_id").ValueGeneratedNever();

        builder.Property(person => person.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(person => person.Email).HasColumnName("email").HasMaxLength(254).IsRequired();

        // Email is the identity of a person, so this is the index that makes the table a directory
        // rather than a list. Every lookup starts here: a session carries an email address, and the
        // person id is found from it.
        builder
            .HasIndex(person => person.Email)
            .HasDatabaseName("uq_person_email")
            .IsUnique();

        // The row meaning "nobody". Seeded here so that every database built from this model has
        // it, which is how the test suite gets it; Liquibase seeds the real one.
        builder.HasData(NoRecord.Person());
    }
}
