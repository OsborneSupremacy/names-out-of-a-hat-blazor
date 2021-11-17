using AutoFixture;
using FluentAssertions;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Test.Utility;
using System;
using System.Collections.Generic;
using Xunit;
using System.Linq;

namespace NamesOutOfAHat2.Service.Tests
{
    public class HatShakerServiceTests
    {
        [Fact]
        public void Should_Return_Invalid_When_No_Resolution()
        {
            // arrange
            var autoFixture = new Fixture().AddAutoMoqCustomization();

            var joe = new Person() { Id = Guid.NewGuid(), Name = "Joe", Email = "Joe@gmail.com" };
            var sue = new Person() { Id = Guid.NewGuid(), Name = "Sue", Email = "Sue@gmail.com" };
            var sam = new Person() { Id = Guid.NewGuid(), Name = "Sam", Email = "Sam@gmail.com" };
            var andy = new Person() { Id = Guid.NewGuid(), Name = "Andy", Email = "Andy@gmail.com" };

            var hat = new Hat()
            {
                Participants = new List<Participant>()
                {
                    new Participant(joe) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (sue, true),
                            new Recipient (sam, true),
                            new Recipient (andy, true),
                        }
                    },

                    new Participant(sue) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (joe, true),
                            new Recipient (sam, false),
                            new Recipient (andy, false),
                        }
                    },

                    new Participant(sam) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (joe, true),
                            new Recipient (sue, false),
                            new Recipient (andy, false)
                        }
                    },

                    new Participant(andy) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (joe, true),
                            new Recipient (sue, false),
                            new Recipient (sam, false),
                        }
                    }
                }
            };

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
        public void Should_Return_Valid_When_One_Resolution_Possible()
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
                            new Recipient (joe, false),
                            new Recipient (sam, true)
                        }
                    },

                    new Participant(sam) {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient (joe, true),
                            new Recipient (sue, false)
                        }
                    }
                }
            };

            var service = autoFixture.Create<HatShakerService>();

            // act
            var (isValid, errors, hatOut) = service.Shake(hat, 1);

            // assert
            isValid.Should().BeTrue();
            hatOut.Participants.Where(x => x.PickedRecipient is null).Should().BeEmpty();
        }
    }
}
