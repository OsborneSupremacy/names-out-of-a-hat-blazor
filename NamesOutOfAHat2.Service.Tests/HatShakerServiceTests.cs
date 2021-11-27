using AutoFixture;
using FluentAssertions;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Test.Utility;
using System.Collections.Generic;
using Xunit;
using System.Linq;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service.Tests
{
    public class HatShakerServiceTests
    {

        private Hat GetUnresolvableHat()
        {
            var joe = "joe".ToPerson();
            var sue = "sue".ToPerson();
            var sam = "sam".ToPerson();
            var andy = "andy".ToPerson();

            var hat = new Hat()
                .AddParticipant(joe.ToParticipant()
                    .Eligible(sue, sam, andy)
                )
                .AddParticipant(sue.ToParticipant()
                    .Eligible(joe)
                    .Ineligible(sam, andy)
                )
                .AddParticipant(sam.ToParticipant()
                    .Eligible(joe)
                    .Ineligible(sue, andy)
                )
                .AddParticipant(andy.ToParticipant()
                    .Eligible(joe)
                    .Ineligible(sue, sam)
                );

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

            var hat = new Hat()
                .AddParticipant(alpha.ToParticipant()
                    .Eligible(beta, charlie, delta, echo, foxtrot, golf, hotel, india, kilo)
                )
                .AddParticipant(beta.ToParticipant()
                    .Eligible(alpha, charlie, delta, echo, foxtrot, golf, hotel, india)
                    .Ineligible(kilo)
                )
                .AddParticipant(charlie.ToParticipant()
                    .Eligible(alpha, beta, delta, echo, foxtrot, golf, hotel, india)
                    .Ineligible(kilo)
                )
                .AddParticipant(delta.ToParticipant()
                    .Eligible(alpha, beta, charlie, echo, foxtrot, golf, hotel, india)
                    .Ineligible(kilo)
                )
                .AddParticipant(echo.ToParticipant()
                    .Eligible(alpha, beta, charlie, delta, foxtrot, golf, hotel, india)
                    .Ineligible(kilo)
                )
                .AddParticipant(foxtrot.ToParticipant()
                    .Eligible(alpha, beta, charlie, delta, echo, golf, hotel, india)
                    .Ineligible(kilo)
                )
                .AddParticipant(golf.ToParticipant()
                    .Eligible(alpha, beta, charlie, delta, echo, foxtrot, hotel, india)
                    .Ineligible(kilo)
                )
                .AddParticipant(hotel.ToParticipant()
                    .Eligible(alpha, beta, charlie, delta, echo, foxtrot, golf, india)
                    .Ineligible(kilo)
                )
                .AddParticipant(india.ToParticipant()
                    .Eligible(alpha, beta, charlie, delta, echo, foxtrot, golf, hotel)
                    .Ineligible(kilo)
                )
                .AddParticipant(kilo.ToParticipant()
                    .Eligible(alpha, beta, charlie, delta, echo, foxtrot, hotel, india, golf)
                );

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
            var (isValid, errors, hatOut) = service.Shake(hat, 1);

            // assert
            isValid.Should().BeFalse();
            errors.Should().NotBeEmpty();
            errors.Should().OnlyHaveUniqueItems();
            hatOut.Participants.Where(x => x.PickedRecipient is not null).Should().BeEmpty();
        }

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
                .AddParticipant(joe.ToParticipant()
                    .Eligible(sue)
                    .Ineligible(sam)
                )
                .AddParticipant(sue.ToParticipant()
                    .Eligible(sam)
                    .Ineligible(joe)
                )
                .AddParticipant(sam.ToParticipant()
                    .Eligible(joe)
                    .Ineligible(sue)
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
    }
}
