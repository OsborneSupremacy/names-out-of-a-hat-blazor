using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class MessageHub : Hub
    {
        public async Task SendGiftExchange(Hat hat)
        {
            var x = hat.Participants.Count();

        }
    }
}
