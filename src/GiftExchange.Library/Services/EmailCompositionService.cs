using System.Text;
using System.Web;

namespace GiftExchange.Library.Services;

[UsedImplicitly]
public class EmailCompositionService
{
    public string ComposeEmail(Hat hat, string participant, string pickedName, string giftIdeasToken)
    {
        // Everything below originates with the organizer or a participant. The subject is plain
        // text and must not be encoded, but every value placed into this HTML body must be.
        var organizerName = HttpUtility.HtmlEncode(hat.Organizer.Name);
        var organizerEmail = HttpUtility.HtmlEncode(hat.Organizer.Email);

        var lines = new List<string>
        {
            EmailBranding.Masthead(),
            $"Dear {HttpUtility.HtmlEncode(participant)},",
            GetGreeting(hat, organizerName, organizerEmail),
            "The person whose name was picked out of a hat for you is:",
            $"<b>{pickedName.GetPersonEmojiFor()} {HttpUtility.HtmlEncode(pickedName)}</b>"
        };

        if (!string.IsNullOrWhiteSpace(hat.PriceRange))
            lines.Add($"Please purchase a gift in the range of {HttpUtility.HtmlEncode(hat.PriceRange)}.");

        if (!string.IsNullOrWhiteSpace(hat.AdditionalInformation))
            lines.Add(HttpUtility.HtmlEncode(hat.AdditionalInformation.Trim()));

        if (!string.IsNullOrWhiteSpace(giftIdeasToken))
        {
            // Ask first, then share. The order matches what somebody opening this actually wants
            // to do: they have just been told a name, and the immediate question is what that
            // person wants, not what they themselves want.
            lines.Add(GiftIdeaEmailCompositionService.BuildAskBlock(pickedName, giftIdeasToken));
            lines.Add(GiftIdeaEmailCompositionService.BuildShareGiftIdeasBlock(giftIdeasToken));
        }

        lines.AddRange([
            $"""If you have any questions, contact <a href="mailto:{HttpUtility.HtmlAttributeEncode(hat.Organizer.Email)}">{organizerName}</a>.""",
            "<i>Please do not reply to this email or share it with anyone else in the gift exchange. Only you know whose name you were assigned!</i>",
            EmailBranding.SignOff(),
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
        $"{organizerName} ({organizerEmail}) has added you to {HttpUtility.HtmlEncode(GiftExchangeNaming.Describe(hat.Name))}!";

    public static string GetSubject(Hat hat) =>
        $"{hat.Organizer.Name} has added you to {GiftExchangeNaming.Describe(hat.Name)}!";

    /// <summary>
    /// The footer every invitation carries: who it came from, what this service checks, and what it
    /// does not stand behind.
    /// </summary>
    /// <remarks>
    /// The second paragraph is a claim about what actually happens, so it is worth keeping true. An
    /// organizer signs in by email before they can send anything, and every piece of text an
    /// organizer supplies that can reach this email — the exchange name, their own name, the
    /// participant names, the price range and the additional information — passes through
    /// <see cref="ContentModerationService"/> on its way in. "Automatically" is doing real work in
    /// that sentence: nobody reads these, and the paragraph after it is what that costs.
    /// </remarks>
    private static string BuildSmallPrint(string organizerEmail) =>
        $"""
         <small style="color:#666666;">
         This email was sent on behalf of {organizerEmail} through <a href="https://namesoutofahat.com">namesoutofahat.com</a>, a free app for running gift exchanges where names are drawn at random.
         <br /><br />
         Organizers confirm their email address before they can send invitations, and everything they write is screened automatically for illegal and inappropriate content.
         <br /><br />
         Beyond those checks, the gift exchange name, the participant names and any additional information are the organizer's own words, and namesoutofahat.com is not responsible for them.
         </small>
         """;
}
