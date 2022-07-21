using NamesOutOfAHat2.Server.Service;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class SendGridEmailServiceTests
{
#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "Not a permanent test")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public async void SentAsync_Should_Work()
    {
        // arrange
        var settings = new Settings() { SenderEmail = "namesoutofahat@osbornesupremacy.com" };

        var configKeys = new ConfigKeys()
    {
        { "sendGrid", "Paste the real API key here" }
    };

        var emailParts = new EmailParts()
        {
            RecipientEmail = "osborne.ben@gmail.com",
            Subject = "Test Sendgrid Email",
            HtmlBody = "<p>This is an html test.</p>"
        };

        var service = new SendGridEmailService(settings, configKeys);

        // act
        Func<Task> serviceDelegate = async () =>
        {
            await service.SendAsync(emailParts);
        };

        // assert
        await serviceDelegate.Should().NotThrowAsync<Exception>();
    }
}
