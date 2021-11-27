using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NamesOutOfAHat2.Client;
using NamesOutOfAHat2.Service;
using NamesOutOfAHat2.Utility;
using Blazored.LocalStorage;
using NamesOutOfAHat2.Client.Service;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddHttpClient();

builder.Services.RegisterServicesInAssembly(typeof(ClientMessenger));
builder.Services.RegisterServicesInAssembly(typeof(ValidationService));

await builder.Build().RunAsync();
