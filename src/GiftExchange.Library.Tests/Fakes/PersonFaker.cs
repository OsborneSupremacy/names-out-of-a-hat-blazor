using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

public sealed class PersonFaker : Faker<Models.Person>
{
    public PersonFaker()
    {
        RuleFor(p => p.Name, FakeValues.Name);
        RuleFor(p => p.Email, FakeValues.Email);
    }
}
