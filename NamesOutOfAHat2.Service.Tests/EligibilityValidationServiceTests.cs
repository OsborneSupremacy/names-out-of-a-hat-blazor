using NamesOutOfAHat2.Test.Utility.Services;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class EligibilityValidationServiceTests
{
    [Fact]
    public void ValidateEligibility_Should_Fail_When_Person_Is_Not_Elgible()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();

        var hat = new HatBuilder()
            .WithParticipant(joe.ToParticipant().AddEligibleRecipients(sue).AddIneligibleRecipients(sam))
            .WithParticipant(sue.ToParticipant().AddEligibleRecipients(joe).AddIneligibleRecipients(sam))
            .WithParticipant(sam.ToParticipant().AddEligibleRecipients(joe).AddIneligibleRecipients(sue))
            .Build();

        var service = autoFixture.Create<EligibilityValidationService>();

        // act
        var result = service.Validate(hat);

        // assert
        result.IsSuccess.Should().BeFalse();
        result.GetErrors().Count().Should().Be(1);
        result.GetErrors().Single().Should().Contain(expected: "sam");
    }

    [Fact]
    public void ValidateEligibility_Should_Succeed_When_Everyone_Elgible()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();

        var hat = new HatBuilder()
            .WithParticipant(joe.ToParticipant().AddEligibleRecipients(sue, sam))
            .WithParticipant(sue.ToParticipant().AddEligibleRecipients(joe).AddIneligibleRecipients(sam))
            .WithParticipant(sam.ToParticipant().AddEligibleRecipients(joe, sue))
            .Build();

        var service = autoFixture.Create<EligibilityValidationService>();

        // act
        var result = service.Validate(hat);

        // assert
        result.IsSuccess.Should().BeTrue();
    }


    [Fact]
    public void ValidateEligibility_Should_Fail_When_Person_Has_No_Recipients()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();

        var hat = new HatBuilder()
            .WithParticipant(joe.ToParticipant())
            .WithParticipant(sue.ToParticipant().AddEligibleRecipients(joe, sam))
            .WithParticipant(sam.ToParticipant().AddEligibleRecipients(joe, sue))
            .Build();

        var service = autoFixture.Create<EligibilityValidationService>();

        // act
        var result = service.Validate(hat);

        // assert
        result.IsSuccess.Should().BeFalse();
        result.GetErrors().Count().Should().Be(1);
        result.GetErrors().Single().Should().Contain("joe");
    }

    [Fact]
    public void ValidateEligibility_Should_Fail_When_Person_Has_No_Elgible_Recipients()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();

        var hat = new HatBuilder()
            .WithParticipant(joe.ToParticipant().AddIneligibleRecipients(sue, sam))
            .WithParticipant(sue.ToParticipant().AddEligibleRecipients(joe, sam))
            .WithParticipant(sam.ToParticipant().AddEligibleRecipients(joe, sue))
            .Build();

        var service = autoFixture.Create<EligibilityValidationService>();

        // act
        var result = service.Validate(hat);

        // assert
        result.IsSuccess.Should().BeFalse();
        result.GetErrors().Count().Should().Be(1);
        result.GetErrors().Single().Should().Contain("joe");
    }
}
