using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;
using System.Text;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class EmailStagingService
    {
        private const string _participantPh = "--|||||participant|||||--";
        private const string _pickedNamePh = "--|||||pickedName|||||--";

        public async Task<List<EmailParts>> StageEmailsAsync(Hat hat)
        {
            var subject = string.Empty;

            if (!string.IsNullOrWhiteSpace(hat.Name))
                subject = $"Thank you for participating in the {hat.Name}!";
            else
                subject = $"{hat.Organizer.Person.Name} has added you to a gift exchange!";

            var emailTemplate = GenerateEmailTemplate(hat, subject);

            var emails = new List<EmailParts>();

            foreach(var participant in hat.Participants)
            {
                emails.Add(new EmailParts()
                { 
                    RecipientEmail = participant.Person.Email,
                    Subject = subject,
                    HtmlBody = emailTemplate
                        .Replace(_participantPh, participant.Person.Name)
                        .Replace(_pickedNamePh, participant.PickedRecipient.Name)
                });
            }

            return emails;
        }

        public string GenerateEmailTemplate(Hat hat, string subject)
        {
            var e = new List<string>();

            e.Add($"Dear {_participantPh},");
            e.Add(subject);
            e.Add("The person you have been randomly assigned is:");
            e.Add($"<b>{_pickedNamePh}</b>");

            if (!string.IsNullOrWhiteSpace(hat.PriceRange))
                e.Add($"Please purchase a gift in the range of {hat.PriceRange}.");

            if (!string.IsNullOrWhiteSpace(hat.AdditionalInformation))
                e.Add(hat.AdditionalInformation);

            e.Add($@"If you have any questions, contact <a href=""mailto:{hat.Organizer.Person.Email}"">{hat.Organizer.Person.Name}</a>");
            e.Add("<i>Please do not reply to this email or share it with anyone else in the gift exchange. Only you know whose name you were assigned!</i>");

            var s = new StringBuilder();
            foreach(var i in e)
            {
                s.Append(i);
                s.AppendLine("<br /><br />");
            }

            return s.ToString();
        }
    }
}
