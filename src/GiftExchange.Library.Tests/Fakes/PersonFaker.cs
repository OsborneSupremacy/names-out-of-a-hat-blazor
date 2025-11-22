using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

public sealed class PersonFaker : Faker<Models.Person>
{
    public PersonFaker()
    {
        RuleFor(p => p.Name, f => f.Person.FirstName);
        RuleFor(p => p.Email, f => f.Person.Email);
    }
}
