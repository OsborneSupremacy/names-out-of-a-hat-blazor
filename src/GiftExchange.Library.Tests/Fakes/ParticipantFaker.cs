using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

public sealed class ParticipantFaker : Faker<Participant>
{
    public ParticipantFaker()
    {
        RuleFor(p => p.Person, _ => new PersonFaker().Generate());
        RuleFor(f => f.PickedRecipient, string.Empty);
        RuleFor(f => f.EligibleRecipients, []);
    }
}
