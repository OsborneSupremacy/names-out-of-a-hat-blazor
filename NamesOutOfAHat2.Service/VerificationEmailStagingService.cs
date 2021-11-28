using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using System.Text;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class VerificationEmailStagingService
    {
        public async Task<EmailParts> StageEmailAsync(Hat hat, string code)
        {
            var e = new List<string>();

            e.Add($"Dear {hat.Organizer.Person.Name},");
            e.Add("Your 🎩 Names Out Of A Hat 🎩 verification code is:");
            e.Add($"<b>{code}</b>");

            var s = new StringBuilder();
            foreach (var i in e)
            {
                s.Append(i);
                s.AppendLine("<br /><br />");
            }

            return new EmailParts()
            {
                RecipientEmail = hat.Organizer.Person.Email,
                Subject = "🎩 Names Out Of A Hat 🎩 Verification Code",
                HtmlBody = s.ToString()
            };
        }
    }
}
