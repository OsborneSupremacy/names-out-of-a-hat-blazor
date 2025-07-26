namespace NamesOutOfAHat2.Model.ViewModels;

public record InvitationViewModel
{
    public required string RecipientEmail { get; set; }

    public required string Subject { get; set; }

    public required string HtmlBody { get; set; }

    public static implicit operator InvitationViewModel(Invitation invitation) => new InvitationViewModel
    {
        RecipientEmail = invitation.RecipientEmail,
        Subject = invitation.Subject,
        HtmlBody = invitation.HtmlBody
    };
}
