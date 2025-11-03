using NamesOutOfAHat2.Model.DomainModels;
using NamesOutOfAHat2.Server.Service;

namespace NamesOutOfAHat2.Service.Tests;

[ExcludeFromCodeCoverage]
public class EmailServiceTests
{
#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "Not a permanent test")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public async Task SendAsync_Should_Work()
    {
        // arrange
        var settings = new Settings { SenderEmail = "nw@namesoutofahat.com", SendEmails = true };

        var configKeys = new ConfigKeys()
        {
            { "SES_ACCESS_KEY", "Copy the real access key here" },
            { "SES_ACCESS_KEY_SECRET", "Copy the access key secret here" },
            { "AWS_REGION", "us-east-1" },
        };

        var emailParts = new Invitation()
        {
            RecipientEmail = "osborne.ben@gmail.com",
            Subject = "Test Sendgrid Email",
            HtmlBody = "<p>This is an html test.</p>"
        };

        var service = new EmailService(settings, configKeys);

        var result = await service.SendAsync(emailParts);

        // assert
        result.IsSuccess.Should().BeTrue();
    }
}
