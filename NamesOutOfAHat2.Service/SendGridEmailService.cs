using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NamesOutOfAHat2.Service
{
    [RegistrationTarget(typeof(IEmailService))]
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey = string.Empty;

        public SendGridEmailService(ApiKeys apiKeys)
        {
            _apiKey = apiKeys?.GetValueOrDefault("sendGrid") ?? throw new ArgumentNullException(nameof(apiKeys));
        }

        public async Task SendAsync(
            string senderEmail,
            string recipientEmail,
            string subject,
            string plainTextContent,
            string htmlContent
            )
        {
            await new SendGridClient(_apiKey)
                .SendEmailAsync(
                    MailHelper
                        .CreateSingleEmail(
                        new EmailAddress(senderEmail), 
                        new EmailAddress(recipientEmail), 
                        subject, 
                        plainTextContent, 
                        htmlContent)
                );
        }
    }
}
