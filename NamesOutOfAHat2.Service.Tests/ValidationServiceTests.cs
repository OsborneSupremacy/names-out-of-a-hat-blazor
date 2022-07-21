using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Test.Utility;
using NamesOutOfAHat2.Utility;
using Xunit;

namespace NamesOutOfAHat2.Service.Tests
{
    [ExcludeFromCodeCoverage]
    public class ValidationServiceTests
    {
        [Fact]
        public void Validate_Should_Fail_When_People_Invalid()
        {
            // arrange
            var autoFixture = new Fixture().AddAutoMoqCustomization();

            IList<Participant> input = new List<Participant>();

            var expectedErrors = new List<string>() { "error1", "error2" };

            //
            autoFixture.Freeze<Mock<IComponentModelValidationService>>()
                .Setup(x => x.ValidateList(It.IsAny<IList<Participant>>()))
                .Returns(new Result<bool>(new MultiException(expectedErrors)));

            var service = autoFixture.Create<ValidationService>();

            // act
            var result = service.Validate(input);

            // assert
            result.IsSuccess.Should().BeFalse();
            result.GetErrors().Should().BeEquivalentTo(expectedErrors);
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
            IList<Participant> input = new List<Participant>();

            input.AddMany(() => { return new Participant(); }, personCount);

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
                .AddScoped<IDuplicateCheckService>(provider => new MockDuplicateCheckService(duplicatesExist))
                .AddScoped(provider => cmvs.Object)
                .BuildServiceProvider();

            IList<Participant> input = new List<Participant>();
            input.AddMany(() => { return new Participant(); }, 3);

            var service = serviceProvider.GetService<ValidationService>();

            // act
            var result = service!.Validate(input);

            // assert
            result.IsSuccess.Should().Be(!duplicatesExist);
            result.GetErrors().Count.Should().Be(expectedErrorCount);
        }

        protected class MockDuplicateCheckService : IDuplicateCheckService
        {
            public bool DuplicatesExist { get; set; }

            public MockDuplicateCheckService(bool returnDuplicatesExist)
            {
                DuplicatesExist = returnDuplicatesExist;
            }

            private List<string> ErrorMessages =>
                DuplicatesExist
                    ? new List<string>() { "Duplicates exist" }
                    : Enumerable.Empty<string>().ToList();

            public Result<bool> Execute(IList<Person> people)
            {
                if (DuplicatesExist)
                    return new Result<bool>(new MultiException(ErrorMessages));
                return true;
            }
        }
    }
}