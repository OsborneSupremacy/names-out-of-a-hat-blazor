using AutoFixture;
using FluentAssertions;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Test.Utility;
using NamesOutOfAHat2.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

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

        var hat = new Hat()
            .AddParticipant(joe.ToParticipant().Eligible(sue).Ineligible(sam))
            .AddParticipant(sue.ToParticipant().Eligible(joe).Ineligible(sam))
            .AddParticipant(sam.ToParticipant().Eligible(joe).Ineligible(sue))
            ;

        var service = autoFixture.Create<EligibilityValidationService>();

        // act
        var (isValid, errors) = service.Validate(hat);

        // assert
        isValid.Should().BeFalse();
        errors.Count().Should().Be(1);
        errors.Single().Should().Contain("sam");
    }

    [Fact]
    public void ValidateEligibility_Should_Succeed_When_Everyone_Elgible()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();

        var hat = new Hat()
            .AddParticipant(joe.ToParticipant().Eligible(sue, sam))
            .AddParticipant(sue.ToParticipant().Eligible(joe).Ineligible(sam))
            .AddParticipant(sam.ToParticipant().Eligible(joe, sue))
            ;

        var service = autoFixture.Create<EligibilityValidationService>();

        // act
        var (isValid, _) = service.Validate(hat);

        // assert
        isValid.Should().BeTrue();
    }


    [Fact]
    public void ValidateEligibility_Should_Fail_When_Person_Has_No_Recipients()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();

        var hat = new Hat()
            .AddParticipant(joe.ToParticipant())
            .AddParticipant(sue.ToParticipant().Eligible(joe, sam))
            .AddParticipant(sam.ToParticipant().Eligible(joe, sue))
            ;

        var service = autoFixture.Create<EligibilityValidationService>();

        // act
        var (isValid, errors) = service.Validate(hat);

        // assert
        isValid.Should().BeFalse();
        errors.Count().Should().Be(1);
        errors.Single().Should().Contain("joe");
    }

    [Fact]
    public void ValidateEligibility_Should_Fail_When_Person_Has_No_Elgible_Recipients()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        var joe = "joe".ToPerson();
        var sue = "sue".ToPerson();
        var sam = "sam".ToPerson();

        var hat = new Hat()
            .AddParticipant(joe.ToParticipant().Ineligible(sue, sam))
            .AddParticipant(sue.ToParticipant().Eligible(joe, sam))
            .AddParticipant(sam.ToParticipant().Eligible(joe, sue))
            ;

        var service = autoFixture.Create<EligibilityValidationService>();

        // act
        var (isValid, errors) = service.Validate(hat);

        // assert
        isValid.Should().BeFalse();
        errors.Count().Should().Be(1);
        errors.Single().Should().Contain("joe");
    }

}
