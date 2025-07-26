using NamesOutOfAHat2.Model.DomainModels;
using NamesOutOfAHat2.Test.Utility.Services;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class HatShakerServiceTests
{
    private Hat GetUnresolvableHat()
    {
        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();
        var andy = "andy".ToPerson();
        // joe can only gift sue, but sue cannot gift joe

        var hat = new HatBuilder()
            .WithParticipant(joe.ToParticipant()
                .AddEligibleRecipients(sue, sam, andy)
            )
            .WithParticipant(sue.ToParticipant()
                .AddEligibleRecipients(joe)
                .AddIneligibleRecipients(sam, andy)
            )
            .WithParticipant(sam.ToParticipant()
                .AddEligibleRecipients(joe)
                .AddIneligibleRecipients(sue, andy)
            )
            .WithParticipant(andy.ToParticipant()
                .AddEligibleRecipients(joe)
                .AddIneligibleRecipients(sue, sam)
            )
            .Build();

        return hat;
    }

    private Hat GetDifficultHat()
    {
        var alpha = "alpha".ToPerson();
        var beta = "beta".ToPerson();
        var charlie = "charlie".ToPerson();
        var delta = "delta".ToPerson();
        var echo = "echo".ToPerson();
        var foxtrot = "foxtrot".ToPerson();
        var golf = "golf".ToPerson();
        var hotel = "hotel".ToPerson();
        var india = "india".ToPerson();

        // will only be eligible to one participant, who is also eligible to gift everyone else
        // If not assigned to that one possible participant, will fail
        var kilo = "kilo".ToPerson();

        var hat = new HatBuilder()
            .WithParticipant(alpha.ToParticipant()
                .AddEligibleRecipients(beta, charlie, delta, echo, foxtrot, golf, hotel, india, kilo)
            )
            .WithParticipant(beta.ToParticipant()
                .AddEligibleRecipients(alpha, charlie, delta, echo, foxtrot, golf, hotel, india)
                .AddIneligibleRecipients(kilo)
            )
            .WithParticipant(charlie.ToParticipant()
                .AddEligibleRecipients(alpha, beta, delta, echo, foxtrot, golf, hotel, india)
                .AddIneligibleRecipients(kilo)
            )
            .WithParticipant(delta.ToParticipant()
                .AddEligibleRecipients(alpha, beta, charlie, echo, foxtrot, golf, hotel, india)
                .AddIneligibleRecipients(kilo)
            )
            .WithParticipant(echo.ToParticipant()
                .AddEligibleRecipients(alpha, beta, charlie, delta, foxtrot, golf, hotel, india)
                .AddIneligibleRecipients(kilo)
            )
            .WithParticipant(foxtrot.ToParticipant()
                .AddEligibleRecipients(alpha, beta, charlie, delta, echo, golf, hotel, india)
                .AddIneligibleRecipients(kilo)
            )
            .WithParticipant(golf.ToParticipant()
                .AddEligibleRecipients(alpha, beta, charlie, delta, echo, foxtrot, hotel, india)
                .AddIneligibleRecipients(kilo)
            )
            .WithParticipant(hotel.ToParticipant()
                .AddEligibleRecipients(alpha, beta, charlie, delta, echo, foxtrot, golf, india)
                .AddIneligibleRecipients(kilo)
            )
            .WithParticipant(india.ToParticipant()
                .AddEligibleRecipients(alpha, beta, charlie, delta, echo, foxtrot, golf, hotel)
                .AddIneligibleRecipients(kilo)
            )
            .WithParticipant(kilo.ToParticipant()
                .AddEligibleRecipients(alpha, beta, charlie, delta, echo, foxtrot, hotel, india, golf)
            )
            .Build();

        return hat;
    }

    [Fact]
    public void Shake_Should_Return_Invalid_When_No_Resolution()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var hat = GetUnresolvableHat();

        var service = autoFixture.Create<HatShakerService>();

        // act
        var result = service.Shake(hat, 1);

        // assert
        result.IsSuccess.Should().BeFalse();
        var errors = result.GetErrors();
        errors.Should().NotBeEmpty().And.OnlyHaveUniqueItems();
    }

    /*
    [Fact]
    public void Shake_Should_Return_Invalid_When_Resolution_Not_Found_On_First_Attempt()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var hat = GetUnresolvableHat();

        var service = autoFixture.Create<HatShakerService>();

        // act
        var (isValid, errors, hatOut) = service.Shake(hat, 1);

        // assert
        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
        errors.Should().OnlyHaveUniqueItems();
        hatOut.Participants.Where(x => x.PickedRecipient is not null).Should().BeEmpty();
    }

    [Fact]
    public void Shake_Should_Return_Valid_When_Resolution_Difficult_But_Possible()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var hat = GetDifficultHat();

        var service = autoFixture.Create<HatShakerService>();

        // act
        var (isValid, errors, hatOut) = service.Shake(hat, 3);

        // assert
        isValid.Should().BeTrue();

        var alpha = hatOut.Participants.Where(x => x.Person.Name == "alpha").First();
        alpha.PickedRecipient.Name.Should().Be("kilo");
    }

    [Fact]
    public void ShakeMultiple_Should_Return_Invalid_When_No_Resolution()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var hat = GetUnresolvableHat();

        var service = autoFixture.Create<HatShakerService>();

        // act
        var (isValid, errors, hatOut) = service.ShakeMultiple(hat, new List<int>() { 1, 2, 3, 4, 5 });

        // assert
        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
        errors.Should().OnlyHaveUniqueItems();
        hatOut.Participants.Where(x => x.PickedRecipient is not null).Should().BeEmpty();
    }

    [Fact]
    public void Shake_Should_Return_Valid_When_One_Resolution_Possible()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();

        var hat = new Hat()
            .WithParticipant(joe.ToParticipant()
                .AddEligibleRecipients(sue)
                .AddIneligibleRecipients(sam)
            )
            .WithParticipant(sue.ToParticipant()
                .AddEligibleRecipients(sam)
                .AddIneligibleRecipients(joe)
            )
            .WithParticipant(sam.ToParticipant()
                .AddEligibleRecipients(joe)
                .AddIneligibleRecipients(sue)
            );

        var service = autoFixture.Create<HatShakerService>();

        // act
        var (isValid, errors, hatOut) = service.Shake(hat, 1);

        // assert
        isValid.Should().BeTrue();
        hatOut.Participants.Where(x => x.PickedRecipient is null).Should().BeEmpty();
    }

    [Fact]
    public void ShakeMultiple_Should_Return_Valid_When_Resolution_Difficult_But_Possible()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var hat = GetDifficultHat();

        var service = autoFixture.Create<HatShakerService>();

        var randomSeeds = new List<int>();
        for (int x = 1; x <= 100; x++)
            randomSeeds.Add(x);

        // act
        var (isValid, errors, hatOut) = service.ShakeMultiple(hat, randomSeeds);

        // assert
        isValid.Should().BeTrue();

        var alpha = hatOut.Participants.Where(x => x.Person.Name == "alpha").First();
        alpha.PickedRecipient.Name.Should().Be("kilo");
    }
    */
}
