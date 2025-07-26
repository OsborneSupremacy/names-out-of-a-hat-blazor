using NamesOutOfAHat2.Model.DomainModels;

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

    public Task<List<Invitation>> StageEmailsAsync(Hat hat)
    {
        var emails = new List<Invitation>();

        foreach (var participant in hat.Participants ?? [])
            emails.Add(new Invitation
            {
                RecipientEmail = participant.Person.Email,
                Subject = _emailCompositionService.GetSubject(hat),
                HtmlBody = _emailCompositionService.GenerateEmail(hat, participant.Person.Name, participant.PickedRecipient?.Name ?? string.Empty, _settings.SiteUrl)
            });

        return Task.FromResult(emails);
    }
}
