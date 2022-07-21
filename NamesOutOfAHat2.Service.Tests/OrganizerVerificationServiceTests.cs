using Microsoft.Extensions.Caching.Memory;
using NamesOutOfAHat2.Server.Service;

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
            Organizer = organizer
        };

        OrganizerRegistration registration = new()
        {
            HatId = hatId,
            OrganizerEmail = organizer.Person.Email,
            VerificationCode = code
        };

        registrationService.Register(hat, code);

        var service = new OrganizerVerificationService(memoryCache);

        // act
        var result = service.Verify(registration);

        memoryCache.TryGetValue(hat.Id, out OrganizerRegistration value);

        // assert
        result.Should().BeTrue();
        value.Verified.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_HatId_Not_Found()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var registrationService = new OrganizerRegistrationService(memoryCache);

        var organizer = "joe".ToPerson().ToParticipant();
        var code = "1234";

        Hat hat = new()
        {
            Id = Guid.NewGuid(),
            Organizer = organizer
        };

        registrationService.Register(hat, code);

        OrganizerRegistration registration = new()
        {
            HatId = Guid.NewGuid(),
            OrganizerEmail = organizer.Person.Email,
            VerificationCode = code
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

        Hat hat = new()
        {
            Id = hatId,
            Organizer = "joe".ToPerson().ToParticipant()
        };

        OrganizerRegistration registration = new()
        {
            HatId = hatId,
            OrganizerEmail = "sam".ToPerson().Email,
            VerificationCode = code
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

        Hat hat = new()
        {
            Id = hatId,
            Organizer = organizer
        };

        OrganizerRegistration registration = new()
        {
            HatId = hatId,
            OrganizerEmail = organizer.Person.Email,
            VerificationCode = "2345"
        };

        registrationService.Register(hat, "1234");

        var service = new OrganizerVerificationService(memoryCache);

        // act
        var result = service.Verify(registration);

        // assert
        result.Should().BeFalse();
    }
}
