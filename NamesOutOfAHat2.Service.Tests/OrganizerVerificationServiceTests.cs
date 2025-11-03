using Microsoft.Extensions.Caching.Memory;
using NamesOutOfAHat2.Model.DomainModels;
using NamesOutOfAHat2.Server.Service;
using NamesOutOfAHat2.Test.Utility.Services;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class OrganizerVerificationServiceTests
{
    [Fact]
    public void Should_Succeed_When_Everything_Matches()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var registrationService = new OrganizerRegistrationService(memoryCache);

        var hatId = Guid.NewGuid();
        var organizer = "joe".ToPerson().ToParticipant();
        var code = "1234";

        Hat hat = new()
        {
            Id = hatId,
            Organizer = organizer,
            Errors = [],
            Name = string.Empty,
            AdditionalInformation = string.Empty,
            PriceRange = string.Empty,
            Participants = []
        };

        OrganizerRegistration registration = new()
        {
            HatId = hatId,
            OrganizerEmail = organizer.Person.Email,
            VerificationCode = code,
            Verified = false
        };

        registrationService.Register(hat, code);

        var service = new OrganizerVerificationService(memoryCache);

        // act
        var result = service.Verify(registration);

        memoryCache.TryGetValue(hat.Id, out OrganizerRegistration? value);

        // assert
        result.Should().BeTrue();
        (value?.Verified ?? false).Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_HatId_Not_Found()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var registrationService = new OrganizerRegistrationService(memoryCache);

        var organizer = "joe".ToPerson().ToParticipant();
        var code = "1234";

        var hat = new HatBuilder()
            .WithId(Guid.NewGuid())
            .WithOrganizer(organizer)
            .Build();

        registrationService.Register(hat, code);

        OrganizerRegistration registration = new()
        {
            HatId = Guid.NewGuid(),
            OrganizerEmail = organizer.Person.Email,
            VerificationCode = code,
            Verified = false
        };

        var service = new OrganizerVerificationService(memoryCache);

        // act
        var result = service.Verify(registration);

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Organizer_Email_Doesnt_Match()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var registrationService = new OrganizerRegistrationService(memoryCache);

        var hatId = Guid.NewGuid();
        var code = "1234";

        var hat = new HatBuilder()
            .WithId(hatId)
            .WithOrganizer("joe".ToPerson().ToParticipant())
            .Build();

        OrganizerRegistration registration = new()
        {
            HatId = hatId,
            OrganizerEmail = "sam".ToPerson().Email,
            VerificationCode = code,
            Verified = false
        };

        registrationService.Register(hat, code);

        var service = new OrganizerVerificationService(memoryCache);

        // act
        var result = service.Verify(registration);

        // assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Verification_Code_Doesnt_Match()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var registrationService = new OrganizerRegistrationService(memoryCache);

        var hatId = Guid.NewGuid();
        var organizer = "joe".ToPerson().ToParticipant();

        var hat = new HatBuilder()
            .WithId(hatId)
            .WithOrganizer(organizer)
            .Build();

        OrganizerRegistration registration = new()
        {
            HatId = hatId,
            OrganizerEmail = organizer.Person.Email,
            VerificationCode = "2345",
            Verified = false
        };

        registrationService.Register(hat, "1234");

        var service = new OrganizerVerificationService(memoryCache);

        // act
        var result = service.Verify(registration);

        // assert
        result.Should().BeFalse();
    }
}
