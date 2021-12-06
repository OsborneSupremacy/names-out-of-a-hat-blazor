using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NamesOutOfAHat2.Server.Service
{
    [RegistrationTarget(typeof(IEmailService))]
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey = string.Empty;

        private readonly string _senderEmail = string.Empty;

        private readonly bool _sendEmails = false;

        public SendGridEmailService(ConfigKeys configKeys)
        {
            _apiKey = configKeys?.GetValueOrDefault("sendGrid") ?? throw new ArgumentNullException(nameof(configKeys));
            _senderEmail = configKeys?.GetValueOrDefault("senderEmail") ?? throw new ArgumentNullException(nameof(configKeys));

            _sendEmails = (configKeys?.GetValueOrDefault("sendEmails", "false") ?? "false")
                .ToBoolOrDefault(false);
        }

        public async Task<(bool success, string details)> SendAsync(EmailParts emailParts)
        {
            if (!_sendEmails)
                return (false, "The application is currently configured to not send emails.");

            var response = await new SendGridClient(_apiKey)
                .SendEmailAsync(
                    MailHelper
                        .CreateSingleEmail(
                        new EmailAddress(_senderEmail), 
                        new EmailAddress(emailParts.RecipientEmail), 
                        emailParts.Subject, 
                        string.Empty, 
                        emailParts.HtmlBody)
                );

            if (response.IsSuccessStatusCode)
                return (true, string.Empty);
            
            var details = $"Error sending email to {emailParts.RecipientEmail}. Code: {response.StatusCode}";

            return (false, details);
        }
    }
}
