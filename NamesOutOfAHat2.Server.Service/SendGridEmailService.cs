namespace NamesOutOfAHat2.Server.Service;

[RegistrationTarget(typeof(IEmailService))]
[ServiceLifetime(ServiceLifetime.Scoped)]
public class SendGridEmailService : IEmailService
{
    private readonly string _apiKey;

    private readonly Settings _settings;

    public SendGridEmailService(
        Settings settings,
        ConfigKeys configKeys
        )
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _apiKey = configKeys?.GetValueOrDefault("sendGrid") ?? throw new ArgumentNullException(nameof(configKeys));
    }

    public async Task<Result<bool>> SendAsync(EmailParts emailParts)
    {
        if (!_settings.SendEmails)
            return new Result<bool>(new InvalidOperationException("The application is currently configured to not send emails."));

        var response = await new SendGridClient(_apiKey)
            .SendEmailAsync(
                MailHelper
                    .CreateSingleEmail(
                        new EmailAddress(_settings.SenderEmail, _settings.SenderName),
                        new EmailAddress(emailParts.RecipientEmail),
                        emailParts.Subject,
                        string.Empty,
                        emailParts.HtmlBody
                    )
            );

        if (response.IsSuccessStatusCode)
            return true;

        var details = $"Error sending email to {emailParts.RecipientEmail}. Code: {response.StatusCode}";

        return new Result<bool>(new AggregateException(details));
    }
}
