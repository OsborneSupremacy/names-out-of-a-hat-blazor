using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamesOutOfAHat2.Interface
{
    public interface IEmailService
    {
        public Task SendAsync(
            string senderEmail,
            string recipientEmail,
            string subject,
            string plainTextContent,
            string htmlContent
        );
    }
}
