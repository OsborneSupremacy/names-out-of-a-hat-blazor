using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using NamesOutOfAHat2.Interface;
using NamesOutOfAHat2.Model;
using NamesOutOfAHat2.Service;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    .Equals("Development", StringComparison.OrdinalIgnoreCase))
    configBuilder.AddUserSecrets<Program>();

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

builder.Services.AddScoped<IEmailService, SendGridEmailService>();
builder.Services.AddScoped<EmailStagingService>();

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

app.MapHub<MessageHub>("/messagehub");
app.MapFallbackToFile("index.html");

app.Run();
