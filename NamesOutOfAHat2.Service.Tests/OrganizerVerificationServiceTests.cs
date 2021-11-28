using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Server.Service;
using NamesOutOfAHat2.Test.Utility;
using NamesOutOfAHat2.Utility;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace NamesOutOfAHat2.Service.Tests
{
    [ExcludeFromCodeCoverage]
    public class OrganizerVerificationServiceTests
    {
        [Fact]
        public void Should_Succeed_When_Everything_Matches()
        {
            // arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var registrationService = new OrganizerRegistrationService(memoryCache);

            Hat orginalHat = new()
            {
                Organizer = "joe".ToPerson().ToParticipant()
            };

            Hat newHat = new()
            {
                Id = orginalHat.Id,
                Organizer = "joe".ToPerson().ToParticipant()
            };

            registrationService.Register(orginalHat, "1234");

            var service = new OrganizerVerificationService(memoryCache);

            // act
            var result = service.Verify(newHat, "1234");

            memoryCache.TryGetValue(orginalHat.Id, out OrganizerRegistration value);

            // assert
            result.Should().BeTrue();
            value.Verified.Should().BeTrue();
        }

        [Fact]
        public void Should_Fail_When_HatId_Not_Found()
        {
            // arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());

            Hat orginalHat = new()
            {
                Organizer = "joe".ToPerson().ToParticipant()
            };

            var service = new OrganizerVerificationService(memoryCache);

            // act
            var result = service.Verify(orginalHat, "1234");

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Should_Fail_When_Organizer_Email_Doesnt_Match()
        {
            // arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var registrationService = new OrganizerRegistrationService(memoryCache);

            Hat hat = new()
            {
                Organizer = "joe".ToPerson().ToParticipant()
            };

            registrationService.Register(hat, "1234");

            hat.Organizer.Person.Email = "joe2@gmail.com";

            var service = new OrganizerVerificationService(memoryCache);

            // act
            var result = service.Verify(hat, "1234");

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Should_Fail_When_Verification_Code_Doesnt_Match()
        {
            // arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var registrationService = new OrganizerRegistrationService(memoryCache);

            Hat hat = new()
            {
                Organizer = "joe".ToPerson().ToParticipant()
            };

            registrationService.Register(hat, "1234");

            var service = new OrganizerVerificationService(memoryCache);

            // act
            var result = service.Verify(hat, "1235");

            // assert
            result.Should().BeFalse();
        }
    }
}
