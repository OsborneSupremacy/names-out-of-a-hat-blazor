namespace NamesOutOfAHat2.Model.DomainModels;

public record Invitation
{
    public required string RecipientEmail { get; init; }

    public required string Subject { get; init; }

    public required string HtmlBody { get; init; }

    public static implicit operator Invitation(InvitationViewModel vm) => new Invitation
    {
        RecipientEmail = vm.RecipientEmail,
        Subject = vm.Subject,
        HtmlBody = vm.HtmlBody
    };
}
