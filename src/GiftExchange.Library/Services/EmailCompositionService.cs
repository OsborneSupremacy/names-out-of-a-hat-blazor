using System.Text;
using System.Web;

namespace GiftExchange.Library.Services;

[UsedImplicitly]
internal class EmailCompositionService
{
    public static string ComposeEmail(Hat hat, string participant, string pickedName)
    {
        var e = new List<string>
        {
            $"Dear {participant},",
            GetSubject(hat),
            "The person whose name was picked out of a hat for you is:",
            $"<b>{pickedName.GetPersonEmojiFor()} {pickedName}</b>"
        };

        if (!string.IsNullOrWhiteSpace(hat.PriceRange))
            e.Add($"Please purchase a gift in the range of {HttpUtility.HtmlEncode(hat.PriceRange)}.");

        if (!string.IsNullOrWhiteSpace(hat.AdditionalInformation))
            e.Add(HttpUtility.HtmlEncode(hat.AdditionalInformation));

        e.Add($"""If you have any questions, contact <a href="mailto:{hat.Organizer.Email}">{hat.Organizer.Name}</a>.""");
        e.Add("<i>Please do not reply to this email or share it with anyone else in the gift exchange. Only you know whose name you were assigned!</i>");
        e.Add("<b>Names Out Of A Hat</b>");

        var s = new StringBuilder();
        foreach (var i in e)
        {
            s.Append(i);
            s.AppendLine("<br /><br />");
        }

        return s.ToString();
    }

    public static string GetSubject(Hat hat) =>
        string.IsNullOrWhiteSpace(hat.Name) switch
        {
            true => $"{hat.Organizer.Name} has added you to a gift exchange!",
            _ => $"Thank you for participating in {GetQualifiedName(hat.Name)}!"
        };

    private static string GetQualifiedName(string name) =>
        !name.StartsWith("the ", StringComparison.OrdinalIgnoreCase) ? name : $"the {name}";
}
