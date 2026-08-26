using System.Text;
using System.Web;

namespace GiftExchange.Library.Services;

[UsedImplicitly]
public class EmailCompositionService
{
    public string ComposeEmail(Hat hat, string participant, string pickedName)
    {
        // Everything below originates with the organizer or a participant. The subject is plain
        // text and must not be encoded, but every value placed into this HTML body must be.
        var organizerName = HttpUtility.HtmlEncode(hat.Organizer.Name);
        var organizerEmail = HttpUtility.HtmlEncode(hat.Organizer.Email);

        var lines = new List<string>
        {
            $"Dear {HttpUtility.HtmlEncode(participant)},",
            GetGreeting(hat, organizerName, organizerEmail),
            "The person whose name was picked out of a hat for you is:",
            $"<b>{pickedName.GetPersonEmojiFor()} {HttpUtility.HtmlEncode(pickedName)}</b>"
        };

        if (!string.IsNullOrWhiteSpace(hat.PriceRange))
            lines.Add($"Please purchase a gift in the range of {HttpUtility.HtmlEncode(hat.PriceRange)}.");

        if (!string.IsNullOrWhiteSpace(hat.AdditionalInformation))
            lines.Add(HttpUtility.HtmlEncode(hat.AdditionalInformation.Trim()));

        lines.AddRange([
            $"""If you have any questions, contact <a href="mailto:{HttpUtility.HtmlAttributeEncode(hat.Organizer.Email)}">{organizerName}</a>.""",
            "<i>Please do not reply to this email or share it with anyone else in the gift exchange. Only you know whose name you were assigned!</i>",
            """<a href="https://namesoutofahat.com"><b>🎩 Names Out Of A Hat 🎩</b></a>""",
            BuildSmallPrint(organizerEmail)
        ]);

        var body = new StringBuilder();

        foreach (var line in lines)
        {
            body.Append(line);
            body.AppendLine("<br /><br />");
        }

        return body.ToString();
    }

    /// <summary>
    /// The body names the organizer's address as well as their name, so a recipient can see who
    /// the exchange actually came from rather than only a display name somebody chose.
    /// </summary>
    private static string GetGreeting(Hat hat, string organizerName, string organizerEmail) =>
        string.IsNullOrWhiteSpace(hat.Name)
            ? $"{organizerName} ({organizerEmail}) has added you to a gift exchange!"
            : $"{organizerName} ({organizerEmail}) has added you to {HttpUtility.HtmlEncode(GetQualifiedName(hat.Name))}!";

    public static string GetSubject(Hat hat) =>
        string.IsNullOrWhiteSpace(hat.Name)
            ? $"{hat.Organizer.Name} has added you to a gift exchange!"
            // GetQualifiedName already supplies the article. This line used to add one as well,
            // which produced "added you to the the Family Christmas!".
            : $"{hat.Organizer.Name} has added you to {GetQualifiedName(hat.Name)}!";

    private static string BuildSmallPrint(string organizerEmail) =>
        $"""
         <small style="color:#666666;">
         This email was sent on behalf of {organizerEmail}, using <a href="https://namesoutofahat.com">namesoutofahat.com</a>, a free randomized-names-type gift exchange facilitation app.
         <br /><br />
         namesoutofahat.com verifies the email address of the gift exchange organizer and uses content filtering to screen for illegal and inappropriate content.
         <br /><br />
         Other than those measures, namesoutofahat.com is not responsible for content provided by the gift exchange organizer, which includes the gift exchange name, participant names, and additional information.
         </small>
         """;

    private static string GetQualifiedName(string name) =>
        name.StartsWith("the ", StringComparison.OrdinalIgnoreCase) ? name : $"the {name}";
}
