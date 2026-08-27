using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

/// <summary>
/// Bogus generates unbounded text, but the columns are bounded and Postgres enforces it where
/// DynamoDB did not. These keep generated values inside the same limits the validators enforce,
/// so the fakers produce data the application would actually accept.
/// </summary>
internal static class FakeValues
{
    /// <summary>
    /// The suffix keeps hat names unique per organizer, which the unique index now requires and
    /// which matters more since every test class shares one database.
    /// </summary>
    public static string HatName(Faker faker) =>
        Truncate($"{faker.Random.Words(2)} {faker.Random.AlphaNumeric(6)}", 50);

    /// <summary>
    /// Bogus derives an address from the name it generated, so two faked people who land on the
    /// same name land on the same address. That used to be a duplicate within one hat at worst.
    /// Now that person.email identifies somebody for the whole application, it would mean two
    /// unrelated test subjects sharing one row — and the second one renaming the first.
    /// </summary>
    public static string Email(Faker faker) =>
        $"{faker.Random.AlphaNumeric(8)}.{faker.Person.Email}".ToLowerInvariant();

    public static string PriceRange(Faker faker) => Truncate(faker.Random.Words(3), 50);

    public static string AdditionalInformation(Faker faker) => Truncate(faker.Random.Words(5), 2000);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength].TrimEnd();
}
