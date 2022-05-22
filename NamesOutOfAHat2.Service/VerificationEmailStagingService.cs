using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using System.Text;

namespace NamesOutOfAHat2.Service;

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
        List<string> e = new()
        {
            $"Dear {hat.Organizer?.Person.Name},",
            "Your 🎩 Names Out Of A Hat 🎩 verification code is:",
            $"<b>{code}</b>",
            $"-<a href=\"{_settings.SiteUrl}\">Names Out Of A Hat</a>"
        };

        StringBuilder s = new();
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
