using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

public sealed class CreateHatRequestFaker : Faker<CreateHatRequest>
{
    public CreateHatRequestFaker()
    {
        RuleFor(f => f.HatName, FakeValues.HatName);
        RuleFor(f => f.OrganizerName, FakeValues.Name);
        // FakeValues.Email rather than Bogus's own, which this faker alone was still using: two
        // faked organizers landing on one address is the collision that helper exists to stop, and
        // a person is now one row for the whole application.
        RuleFor(f => f.OrganizerEmail, FakeValues.Email);
    }
}
