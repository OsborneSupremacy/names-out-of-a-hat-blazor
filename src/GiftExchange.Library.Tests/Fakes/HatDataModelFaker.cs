using Bogus;

namespace GiftExchange.Library.Tests.Fakes;

public sealed class HatDataModelFaker : Faker<HatDataModel>
{
    public HatDataModelFaker()
    {
        RuleFor(f => f.HatId, f => f.Random.Guid());
        RuleFor(f => f.OrganizerName, FakeValues.Name);
        RuleFor(f => f.OrganizerEmail, FakeValues.Email);
        RuleFor(f => f.HatName, FakeValues.HatName);
        RuleFor(f => f.Status, HatStatus.InProgress);
        RuleFor(f => f.AdditionalInformation, FakeValues.AdditionalInformation);
        RuleFor(f => f.PriceRange, FakeValues.PriceRange);
    }
}
