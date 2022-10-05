using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NamesOutOfAHat2.Client;
using NamesOutOfAHat2.Client.Service;
using NamesOutOfAHat2.Service;
using OsborneSupremacy.Extensions.Net.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddHttpClient();

builder.Services.RegisterServicesInAssembly(typeof(ClientHttpService));
builder.Services.RegisterServicesInAssembly(typeof(ValidationService));

await builder.Build().RunAsync();
