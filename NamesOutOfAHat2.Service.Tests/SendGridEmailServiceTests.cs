using FluentAssertions;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Server.Service;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace NamesOutOfAHat2.Service.Tests
{
    [ExcludeFromCodeCoverage]
    public class SendGridEmailServiceTests
    {
        [Fact(Skip = "Not a permament test")]
        public async void SentAsync_Should_Work()
        {
            // arrange
            var configKeys = new ConfigKeys()
            {
                { "sendGrid", "Paste the real API key here" },
                { "senderEmail", "ben@osbornesupremacy.com" }
            };

            var emailParts = new EmailParts()
            {
                RecipientEmail = "osborne.ben@gmail.com",
                Subject = "Test Sendgrid Email",
                HtmlBody = "<p>This is an html test.</p>"
            };

            var service = new SendGridEmailService(configKeys);

            // act
            Func<Task> serviceDelegate = async () =>
            {
                await service.SendAsync(emailParts);
            };

            // assert
            await serviceDelegate.Should().NotThrowAsync<Exception>();
        }
    }
}
