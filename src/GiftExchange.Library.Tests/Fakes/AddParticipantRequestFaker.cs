using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

public sealed class AddParticipantRequestFaker : Faker<AddParticipantRequest>
{
    public AddParticipantRequestFaker()
    {
        RuleFor(f => f.OrganizerEmail, FakeValues.Email);
        RuleFor(f => f.HatId, f => f.Random.Guid());
        RuleFor(f => f.Name, f => f.Person.FirstName);
        RuleFor(f => f.Email, FakeValues.Email);
    }
}
