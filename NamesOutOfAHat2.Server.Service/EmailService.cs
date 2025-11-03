using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using NamesOutOfAHat2.Model.DomainModels;

namespace NamesOutOfAHat2.Server.Service;

[RegistrationTarget(typeof(IEmailService))]
[ServiceLifetime(ServiceLifetime.Scoped)]
public class EmailService : IEmailService
{
    private readonly string _sesAcessKey;

    private readonly string _sesAccessKeySecret;

    private readonly string _awsRegion;

    private readonly Settings _settings;

    public EmailService(
        Settings settings,
        ConfigKeys configKeys
        )
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _sesAcessKey = configKeys?.GetValueOrDefault("SES_ACCESS_KEY") ?? throw new ArgumentNullException(nameof(configKeys));
        _sesAccessKeySecret = configKeys?.GetValueOrDefault("SES_ACCESS_KEY_SECRET") ?? throw new ArgumentNullException(nameof(configKeys));
        _awsRegion = configKeys?.GetValueOrDefault("AWS_REGION") ?? "us-east-1";
    }

    public async Task<Result<bool>> SendAsync(Invitation invitation)
    {
        if (!_settings.SendEmails)
            return new Result<bool>(new InvalidOperationException("The application is currently configured to not send emails."));

        using var client = new AmazonSimpleEmailServiceClient(_sesAcessKey, _sesAccessKeySecret, _awsRegion);

        var sendRequest = new SendEmailRequest
        {
            Source = _settings.SenderEmail, // _settings.SenderName
            Destination = new Destination
            {
                ToAddresses = [invitation.RecipientEmail]
            },
            Message = new Message
            {
                Subject = new Content(invitation.Subject),
                Body = new Body
                {
                    Html = new Content(invitation.HtmlBody)
                }
            }
        };

        var response = await client.SendEmailAsync(sendRequest);

        if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            return true;

        var details = $"Error sending email to {invitation.RecipientEmail}. Code: {response.HttpStatusCode}. Response: {response}";

        return new Result<bool>(new AggregateException(details));
    }
}
