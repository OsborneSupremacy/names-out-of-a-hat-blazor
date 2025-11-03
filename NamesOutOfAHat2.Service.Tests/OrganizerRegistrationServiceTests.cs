using Microsoft.Extensions.Caching.Memory;
using NamesOutOfAHat2.Model.DomainModels;
using NamesOutOfAHat2.Server.Service;
using NamesOutOfAHat2.Test.Utility.Services;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class OrganizerRegistrationServiceTests
{
    [Fact]
    public void Expected_Behavior()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        Hat hat = new HatBuilder()
            .WithOrganizer("joe".ToPerson().ToParticipant())
            .Build();

        var service = new OrganizerRegistrationService(memoryCache);

        // act
        service.Register(hat, "1234");

        // assert
        memoryCache.TryGetValue(hat.Id, out OrganizerRegistration? value);

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

        Hat hat = new HatBuilder()
            .WithOrganizer("joe".ToPerson().ToParticipant())
            .Build();

        var service = new OrganizerRegistrationService(memoryCache);

        // act
        service.Register(hat, "1234");
        hat = hat with { Organizer = "sam".ToPerson().ToParticipant() };
        service.Register(hat, "5678");

        // assert
        memoryCache.TryGetValue(hat.Id, out OrganizerRegistration? value);

        value.Should().NotBeNull();
        value.HatId.Should().Be(hat.Id);
        value.OrganizerEmail.Should().Be("sam@gmail.com");
        value.VerificationCode.Should().Be("5678");
    }

    [Fact]
    public async Task Should_Fail_When_Not_Valid()
    {
        // arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var hat = new HatBuilder()
            .WithId(Guid.Empty)
            .WithOrganizer("joe".ToPerson().ToParticipant())
            .Build();

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
