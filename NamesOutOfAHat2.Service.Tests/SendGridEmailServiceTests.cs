using FluentAssertions;
using NamesOutOfAHat2.Model;
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
            var apiKeys = new ApiKeys()
            {
                { "sendGrid", "Paste the real API key here" }
            };

            var emailParts = new EmailParts()
            {
                SenderEmail = "ben@osbornesupremacy.com",
                RecipientEmail = "osborne.ben@gmail.com",
                Subject = "Test Sendgrid Email",
                PlainTextBody = "This is a plain text test",
                HtmlBody = "<p>This is an html test.</p>"
            };

            var service = new SendGridEmailService(apiKeys);

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
