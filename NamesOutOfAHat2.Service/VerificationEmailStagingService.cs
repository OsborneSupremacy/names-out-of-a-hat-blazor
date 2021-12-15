using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using System.Text;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class VerificationEmailStagingService
    {
        private readonly Settings _settings;

        public VerificationEmailStagingService(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task<EmailParts> StageEmailAsync(Hat hat, string code)
        {
            var e = new List<string>();

            e.Add($"Dear {hat.Organizer?.Person.Name},");
            e.Add("Your 🎩 Names Out Of A Hat 🎩 verification code is:");
            e.Add($"<b>{code}</b>");
            e.Add($"-<a href=\"{_settings.SiteUrl}\">Names Out Of A Hat</a>");

            var s = new StringBuilder();
            foreach (var i in e)
            {
                s.Append(i);
                s.AppendLine("<br /><br />");
            }

            return Task.FromResult(new EmailParts()
            {
                RecipientEmail = hat.Organizer?.Person?.Email ?? string.Empty,
                Subject = "🎩 Names Out Of A Hat 🎩 Verification Code",
                HtmlBody = s.ToString()
            });
        }
    }
}
