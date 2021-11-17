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
    public static class HatShakerTestExtensions
    {
        public static Person ToPerson(this string name) =>
            new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = $"{name}@gmail.com"
            };

        public static Participant ToParticipant(this Person input) =>
            new(input);

        public static Participant AddRecipient(this Participant input, Person person, bool eligible)
        {
            input.Recipients ??= new List<Recipient>();
            input.Recipients.Add(new Recipient()
            {
                Person = person,
                Eligible = eligible
            });
            return input;
        }

        public static Participant AddEligibleRecipients(this Participant input, params Person[] people) 
        { 
            foreach(var person in people)
                input.AddRecipient(person, true);
            return input;
        }

        public static Participant AddIneligibleRecipients(this Participant input, params Person[] people)
        {
            foreach (var person in people)
                input.AddRecipient(person, false);
            return input;
        }

        public static List<Participant> BuildParticipantList(this List<Participant> input)
        {
            input = new List<Participant>();
            return input;
        }

        public static List<Participant> AddParticipant(this List<Participant> input, Participant participant)
        {
            input.Add(participant);
            return input;
        }
    }

    public class HatShakerServiceTests
    {
        protected List<Participant> BuildParticipantList() => 
            new List<Participant>();

        private Hat GetUnresolvableHat()
        {
            var joe = "joe".ToPerson();
            var sue = "sue".ToPerson();
            var sam = "sam".ToPerson();
            var andy = "andy".ToPerson();

            var hat = new Hat
            {
                Participants = BuildParticipantList()
                .AddParticipant(joe.ToParticipant()
                    .AddEligibleRecipients(sue, sam, andy)
                )
                .AddParticipant(sue.ToParticipant()
                    .AddEligibleRecipients(joe)
                    .AddIneligibleRecipients(sam, andy)
                )
                .AddParticipant(sam.ToParticipant()
                    .AddEligibleRecipients(joe)
                    .AddIneligibleRecipients(sue, andy)
                )
                .AddParticipant(andy.ToParticipant()
                    .AddEligibleRecipients(joe)
                    .AddIneligibleRecipients(sue, sam)
                )
            };

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

            var hat = new Hat
            {
                Participants = BuildParticipantList()
                .AddParticipant(alpha.ToParticipant()
                    .AddEligibleRecipients(beta, charlie, delta, echo, foxtrot, golf, hotel, india, kilo)
                )
                .AddParticipant(beta.ToParticipant()
                    .AddEligibleRecipients(alpha, charlie, delta, echo, foxtrot, golf, hotel, india)
                    .AddIneligibleRecipients(kilo)
                )
                .AddParticipant(charlie.ToParticipant()
                    .AddEligibleRecipients(alpha, beta, delta, echo, foxtrot, golf, hotel, india)
                    .AddIneligibleRecipients(kilo)
                )
                .AddParticipant(delta.ToParticipant()
                    .AddEligibleRecipients(alpha, beta, charlie, echo, foxtrot, golf, hotel, india)
                    .AddIneligibleRecipients(kilo)
                )
                .AddParticipant(echo.ToParticipant()
                    .AddEligibleRecipients(alpha, beta, charlie, delta, foxtrot, golf, hotel, india)
                    .AddIneligibleRecipients(kilo)
                )
                .AddParticipant(foxtrot.ToParticipant()
                    .AddEligibleRecipients(alpha, beta, charlie, delta, echo, golf, hotel, india)
                    .AddIneligibleRecipients(kilo)
                )
                .AddParticipant(golf.ToParticipant()
                    .AddEligibleRecipients(alpha, beta, charlie, delta, echo, foxtrot, hotel, india)
                    .AddIneligibleRecipients(kilo)
                )
                .AddParticipant(hotel.ToParticipant()
                    .AddEligibleRecipients(alpha, beta, charlie, delta, echo, foxtrot, golf, india)
                    .AddIneligibleRecipients(kilo)
                )
                .AddParticipant(india.ToParticipant()
                    .AddEligibleRecipients(alpha, beta, charlie, delta, echo, foxtrot, golf, hotel)
                    .AddIneligibleRecipients(kilo)
                )
                .AddParticipant(kilo.ToParticipant()
                    .AddEligibleRecipients(alpha, beta, charlie, delta, echo, foxtrot, hotel, india, golf)
                )
            };

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

            var hat = new Hat
            {
                Participants = BuildParticipantList()
                .AddParticipant(joe.ToParticipant()
                    .AddEligibleRecipients(sue)
                    .AddIneligibleRecipients(sam)
                )
                .AddParticipant(sue.ToParticipant()
                    .AddEligibleRecipients(sam)
                    .AddIneligibleRecipients(joe)
                )
                .AddParticipant(sam.ToParticipant()
                    .AddEligibleRecipients(joe)
                    .AddIneligibleRecipients(sue)
                )
            };

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
