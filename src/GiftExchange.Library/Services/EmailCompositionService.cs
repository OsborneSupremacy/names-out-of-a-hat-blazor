using System.Text;
using System.Web;

namespace GiftExchange.Library.Services;

[UsedImplicitly]
public class EmailCompositionService
{
    /// <summary>
    /// Where gift ideas are received. A subdomain of its own, not the one invitations are sent
    /// from: an MX record there would also catch the DMARC reports that already arrive at
    /// mail.namesoutofahat.com, and SES receipt rules match a whole domain or one exact address,
    /// with no way to claim a prefix.
    /// </summary>
    private const string GiftIdeasDomain = "ideas.namesoutofahat.com";

    public string ComposeEmail(Hat hat, string participant, string pickedName, string giftIdeasToken)
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

        if (!string.IsNullOrWhiteSpace(giftIdeasToken))
            lines.Add(BuildGiftIdeasInvitation(giftIdeasToken));

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
    /// The block inviting this participant to share gift ideas, addressed to the token issued to
    /// them.
    /// </summary>
    /// <remarks>
    /// A <c>mailto:</c> link rather than a reply, and that distinction is doing security work
    /// rather than cosmetic work. This email tells the participant whose name they drew. A reply
    /// would quote it, the quoted text would be hard to strip reliably across mail clients, and
    /// what leaked would be the sender's own pick, forwarded to the one person who must not learn
    /// it. Clicking here opens an empty message instead, so there is nothing to quote and nothing
    /// to strip. The "do not reply" line further down stays true and stays necessary: it is what
    /// steers somebody away from the reply button and towards this.
    ///
    /// The address appears in full underneath, because a mail client that has not been registered
    /// as the handler for mailto: links does nothing at all when this is clicked, with no error to
    /// explain the silence.
    /// </remarks>
    private static string BuildGiftIdeasInvitation(string giftIdeasToken)
    {
        var address = $"{giftIdeasToken}@{GiftIdeasDomain}";
        var mailto = $"mailto:{HttpUtility.UrlEncode(giftIdeasToken)}@{GiftIdeasDomain}?subject=My%20gift%20ideas";

        return $"""
                <a href="{HttpUtility.HtmlAttributeEncode(mailto)}" style="background-color:#1f7a4d;color:#ffffff;padding:12px 22px;text-decoration:none;border-radius:4px;display:inline-block;font-weight:bold;">SHARE GIFT IDEAS</a>
                <br /><br />
                Click above to share gift ideas with only the person who picked your name. Nobody else in the exchange will see them &mdash; not even the organizer. Your email will open with the address already filled in; just type your ideas and send. We'll email you back to confirm exactly what was shared.
                <br /><br />
                <small style="color:#666666;">Button not working? Send your ideas to {HttpUtility.HtmlEncode(address)}</small>
                """;
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

    private static string GetQualifiedName(string name) =>
        name.StartsWith("the ", StringComparison.OrdinalIgnoreCase) ? name : $"the {name}";
}
