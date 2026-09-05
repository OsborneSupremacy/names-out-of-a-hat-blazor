using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

public sealed class ParticipantFaker : Faker<Participant>
{
    public ParticipantFaker()
    {
        RuleFor(p => p.Person, _ => new PersonFaker().Generate());
        RuleFor(f => f.PickedRecipient, string.Empty);
        RuleFor(f => f.EligibleRecipients, []);
        // Nothing heard. A faked participant has had nothing sent to them, which is what an empty
        // status means -- see the remarks on Participant.DeliveryStatus.
        RuleFor(f => f.DeliveryStatus, DeliveryStatus.Unknown);
        RuleFor(f => f.DeliveryDetail, string.Empty);
        RuleFor(f => f.DeliveryMessageType, string.Empty);
        RuleFor(f => f.DeliveryOccurredAt, DateTimeOffset.MinValue);
    }
}
