using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

public sealed class CreateHatRequestFaker : Faker<CreateHatRequest>
{
    public CreateHatRequestFaker()
    {
        RuleFor(f => f.HatName, f => f.Random.Words(5));
        RuleFor(f => f.OrganizerName, f => f.Person.FirstName);
        RuleFor(f => f.OrganizerEmail, f => f.Person.Email);
    }
}
