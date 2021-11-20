using Microsoft.AspNetCore.SignalR;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;

namespace NamesOutOfAHat2.Service
{
    public class MessageHub : Hub
    {
        private readonly EmailStagingService _emailStagingService;

        private readonly IEmailService _emailService;

        public MessageHub(EmailStagingService emailStagingService, IEmailService emailService)
        {
            _emailStagingService = emailStagingService ?? throw new ArgumentNullException(nameof(emailStagingService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task RecieveGiftExchange(Hat hat)
        {
            var emails = await _emailStagingService.StageEmailsAsync(hat);

#if !DEBUG
            var tasks = new List<Task>();

            foreach (var email in emails)
                tasks.Add(_emailService.SendAsync(email));

            await Task.WhenAll(tasks);
#endif
        }
    }
}
