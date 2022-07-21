using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Server.Service;
using NamesOutOfAHat2.Test.Utility;
using NamesOutOfAHat2.Utility;
using Xunit;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class OrganizerRegistrationServiceTests
{
    [Fact]
    public void Expected_Behavior()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        Hat hat = new()
        {
            Organizer = "joe".ToPerson().ToParticipant()
        };

        var service = new OrganizerRegistrationService(memoryCache);

        // act
        service.Register(hat, "1234");

        // assert
        memoryCache.TryGetValue(hat.Id, out OrganizerRegistration value);

        value.Should().NotBeNull();
        value.HatId.Should().Be(hat.Id);
        value.OrganizerEmail.Should().Be("joe@gmail.com");
        value.VerificationCode.Should().Be("1234");
    }

    [Fact]
    public void Should_Displace_Existing()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        Hat hat = new()
        {
            Organizer = "joe".ToPerson().ToParticipant()
        };

        var service = new OrganizerRegistrationService(memoryCache);

        // act
        service.Register(hat, "1234");
        hat.Organizer = "sam".ToPerson().ToParticipant();
        service.Register(hat, "5678");

        // assert
        memoryCache.TryGetValue(hat.Id, out OrganizerRegistration value);

        value.Should().NotBeNull();
        value.HatId.Should().Be(hat.Id);
        value.OrganizerEmail.Should().Be("sam@gmail.com");
        value.VerificationCode.Should().Be("5678");
    }

    [Fact]
    public async void Should_Fail_When_Not_Valid()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        Hat hat = new()
        {
            Id = Guid.Empty,
            Organizer = "joe".ToPerson().ToParticipant()
        };

        var service = new OrganizerRegistrationService(memoryCache);

        // act
        var serviceDelegate = async () =>
        {
            await Task.Run(() =>
            {
                service.Register(hat, "1234");
            });
        };

        // assert
        await serviceDelegate.Should().ThrowAsync<ArgumentException>();
    }
}
