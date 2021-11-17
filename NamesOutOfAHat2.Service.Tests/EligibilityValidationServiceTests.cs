using AutoFixture;
using FluentAssertions;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Test.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NamesOutOfAHat2.Service.Tests
{
    public class EligibilityValidationServiceTests
    {
        [Fact]
        public void ValidateEligibility_Should_Fail_When_Person_Is_Not_Elgible()
        {
            // arrange
            var autoFixture = new Fixture().AddAutoMoqCustomization();

            var joe = new Person() { Id = Guid.NewGuid(), Name = "Joe", Email = "Joe@gmail.com" };
            var sue = new Person() { Id = Guid.NewGuid(), Name = "Sue", Email = "Sue@gmail.com" };
            var sam = new Person() { Id = Guid.NewGuid(), Name = "Sam", Email = "Sam@gmail.com" };

            var hat = new Hat()
            {
                Participants = new List<Participant>()
                {
                    new Participant(joe) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (sue, true),
                            new Recipient (sam, false)
                        }
                    },

                    new Participant(sue) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (joe, true),
                            new Recipient (sam, false)
                        }
                    },

                    new Participant(sam) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (joe, true),
                            new Recipient (sue, true)
                        }
                    }
                }
            };

            var service = autoFixture.Create<EligibilityValidationService>();

            // act
            var (isValid, errors) = service.Validate(hat);

            // assert
            isValid.Should().BeFalse();
            errors.Count().Should().Be(1);
            errors.Single().Should().Contain("Sam");
        }

        [Fact]
        public void ValidateEligibility_Should_Succeed_When_Everyone_Elgible()
        {
            // arrange
            var autoFixture = new Fixture().AddAutoMoqCustomization();

            var joe = new Person() { Id = Guid.NewGuid(), Name = "Joe", Email = "Joe@gmail.com" };
            var sue = new Person() { Id = Guid.NewGuid(), Name = "Sue", Email = "Sue@gmail.com" };
            var sam = new Person() { Id = Guid.NewGuid(), Name = "Sam", Email = "Sam@gmail.com" };

            var hat = new Hat()
            {
                Participants = new List<Participant>()
                {
                    new Participant(joe) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (sue, true),
                            new Recipient (sam, true)
                        }
                    },

                    new Participant(sue) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (joe, true),
                            new Recipient (sam, false)
                        }
                    },

                    new Participant(sam) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (joe, true),
                            new Recipient (sue, true)
                        }
                    }
                }
            };

            var service = autoFixture.Create<EligibilityValidationService>();

            // act
            var (isValid, _) = service.Validate(hat);

            // assert
            isValid.Should().BeTrue();
        }

    }
}
