using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Server.Service;
using NamesOutOfAHat2.Service;
using NamesOutOfAHat2.Utility;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");

configBuilder.AddUserSecrets<Program>(true);

// If you have a an Azure App config, when running locally, can store it in launchSettings.json
// On Azure, store it in application settings
var azureConfigConnectionString = Environment
    .GetEnvironmentVariable("AZURE_CONFIG_CONNECTIONSTRING") ?? string.Empty;

if(!string.IsNullOrWhiteSpace(azureConfigConnectionString))
    configBuilder.AddAzureAppConfiguration(azureConfigConnectionString);

var config = configBuilder.Build();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddControllers();

builder.Services.Configure<ConfigKeys>(
    config.GetSection(nameof(ConfigKeys)));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<ConfigKeys>>().Value);

builder.Services.RegisterServicesInAssembly(typeof(ValidationService));
builder.Services.RegisterServicesInAssembly(typeof(SendGridEmailService));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints => {
    endpoints.MapControllers();
});

app.MapFallbackToFile("index.html");

app.Run();
