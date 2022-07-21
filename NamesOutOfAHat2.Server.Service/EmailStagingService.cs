using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Service;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Server.Service;

[ServiceLifetime(ServiceLifetime.Scoped)]
public class EmailStagingService
{
    private readonly Settings _settings;

    private readonly EmailCompositionService _emailCompositionService;

    public EmailStagingService(Settings settings, EmailCompositionService emailCompositionService)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _emailCompositionService = emailCompositionService ?? throw new ArgumentNullException(nameof(emailCompositionService));
    }

    public Task<List<EmailParts>> StageEmailsAsync(Hat hat)
    {
        var emails = new List<EmailParts>();

        foreach (var participant in hat.Participants ?? Enumerable.Empty<Participant>())
            emails.Add(new EmailParts()
            {
                RecipientEmail = participant.Person.Email,
                Subject = _emailCompositionService.GetSubject(hat),
                HtmlBody = _emailCompositionService.GenerateEmail(hat, participant.Person.Name, participant.PickedRecipient?.Name ?? string.Empty, _settings.SiteUrl)
            });

        return Task.FromResult(emails);
    }
}
