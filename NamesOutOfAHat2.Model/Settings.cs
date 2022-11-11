namespace NamesOutOfAHat2.Model;

public record Settings
{
    public bool SendEmails { get; set; }

    public string SenderName { get; set; } = default!;

    public string SiteUrl { get; set; } = default!;

    public string SenderEmail { get; set; } = default!;
}

public class SettingsValidator : AbstractValidator<Settings>
{
    public SettingsValidator()
    {
        RuleFor(x => x.SenderName).NotEmpty();
        RuleFor(x => x.SiteUrl)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _));
        RuleFor(x => x.SenderEmail)
            .NotEmpty()
            .EmailAddress();
    }
}
