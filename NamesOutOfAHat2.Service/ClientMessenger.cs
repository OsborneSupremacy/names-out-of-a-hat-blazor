using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Utility;

namespace NamesOutOfAHat2.Service
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ClientMessenger
    {
        public async Task SendAsync(NavigationManager navigationManager, Hat hat)
        {
            var hubConnection = new HubConnectionBuilder()
                .WithUrl(navigationManager.ToAbsoluteUri("/messagehub"))
                .Build();

            try
            {
                await hubConnection.StartAsync();
                await hubConnection.SendAsync("SendGiftExchange", hat);
                await hubConnection.StopAsync();
            }
            catch
            {
                throw;
            }
            finally
            {
                await hubConnection.DisposeAsync();
            }
        }
    }
}
