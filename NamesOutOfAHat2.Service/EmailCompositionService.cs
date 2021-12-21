using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using System.Text;
using System.Web;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class EmailCompositionService
    {
        public string GenerateEmail(Hat hat, string participant, string pickedName, string siteUrl)
        {
            var e = new List<string>();

            e.Add($"Dear {participant},");
            e.Add(GetSubject(hat));
            e.Add("The person whose name was picked out of a hat for you is:");
            e.Add($"<b>{pickedName}</b>");

            if (!string.IsNullOrWhiteSpace(hat.PriceRange))
                e.Add($"Please purchase a gift in the range of {HttpUtility.HtmlEncode(hat.PriceRange)}.");

            if (!string.IsNullOrWhiteSpace(hat.AdditionalInformation))
                e.Add(HttpUtility.HtmlEncode(hat.AdditionalInformation));

            e.Add($@"If you have any questions, contact <a href=""mailto:{hat.Organizer?.Person.Email ?? string.Empty}"">{hat.Organizer?.Person.Name ?? string.Empty}</a>");
            e.Add("<i>Please do not reply to this email or share it with anyone else in the gift exchange. Only you know whose name you were assigned!</i>");
            
            if(string.IsNullOrWhiteSpace(siteUrl))
                e.Add($"-<b>Names Out Of A Hat</b>");
            else
                e.Add($"-<a href=\"{siteUrl}\">Names Out Of A Hat</a>");

            var s = new StringBuilder();
            foreach (var i in e)
            {
                s.Append(i);
                s.AppendLine("<br /><br />");
            }

            return s.ToString();
        }

        public string GetSubject(Hat hat) =>
            string.IsNullOrWhiteSpace(hat.Name) switch
            {
                true => $"{hat.Organizer?.Person.Name ?? string.Empty} has added you to a gift exchange!",
                _ => $"Thank you for participating in {hat.Name}!"
            };
    }
}
