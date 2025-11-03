using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class ValidationServiceTests
{
    [Fact]
    public void Validate_Should_Fail_When_People_Invalid()
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();

        IList<Participant> input = new List<Participant>();

        var expectedErrors = new List<ValidationException> { 
            new("error1"),
            new("error2") 
        };

        //
        autoFixture.Freeze<Mock<IComponentModelValidationService>>()
            .Setup(x => x.ValidateList(It.IsAny<IList<Participant>>()))
            .Returns(new Result<bool>(new AggregateException(expectedErrors)));

        List<Exception> errors = [];

        var service = autoFixture.Create<ValidationService>();

        // act
        var isSuccess = service.Validate(input)
            .Match(
                _ => true,
                error =>
                {
                    if (error is AggregateException aggregateException)
                        errors = aggregateException.InnerExceptions.ToList();
                    return false;
                }
            );

        // assert
        isSuccess.Should().BeFalse();
        errors.Should().BeEquivalentTo(expectedErrors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(31)]
    public void Validate_Should_Fail_When_Invalid_Person_Count(int personCount)
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();
        var input = autoFixture.CreateMany<Participant>(personCount).ToList();

        autoFixture.Freeze<Mock<IComponentModelValidationService>>()
            .Setup(x => x.ValidateList(It.IsAny<IList<Participant>>()))
            .Returns(true);

        var service = autoFixture.Create<ValidationService>();

        // act
        var result = service.Validate(input);

        // assert
        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void Validate_Expected_Behavior(bool duplicatesExist, int expectedErrorCount)
    {
        // arrange
        var autoFixture = new Fixture().AddAutoMoqCustomization();
        var cmvs = autoFixture.Freeze<Mock<IComponentModelValidationService>>();

        cmvs.Setup(x => x.ValidateList(It.IsAny<IList<Participant>>()))
            .Returns(true);

        using var serviceProvider = new ServiceCollection()
            .AddScoped(typeof(ValidationService))
            .AddScoped<IDuplicateCheckService>(_ => new MockDuplicateCheckService(duplicatesExist))
            .AddScoped(_ => cmvs.Object)
            .BuildServiceProvider();

        var input = autoFixture.CreateMany<Participant>(3).ToList();

        var service = serviceProvider.GetService<ValidationService>();

        // act
        var result = service!.Validate(input);

        // assert
        result.IsSuccess.Should().Be(!duplicatesExist);
        result.GetErrors().Count.Should().Be(expectedErrorCount);
    }

    private class MockDuplicateCheckService : IDuplicateCheckService
    {
        private bool DuplicatesExist { get; set; }

        public MockDuplicateCheckService(bool returnDuplicatesExist)
        {
            DuplicatesExist = returnDuplicatesExist;
        }

        private List<ValidationException> ErrorMessages =>
            DuplicatesExist
                ? [new("Duplicates exist")]
                : Enumerable.Empty<ValidationException>().ToList();

        public Result<bool> Execute(IList<Person> people)
        {
            if (DuplicatesExist)
                return new Result<bool>(new AggregateException(ErrorMessages));
            return true;
        }
    }
}
