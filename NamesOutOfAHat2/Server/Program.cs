using OsborneSupremacy.Extensions.Net.DependencyInjection;
using OsborneSupremacy.Extensions.AspNet;
using FluentValidation;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");

configBuilder.AddUserSecrets<Program>(true);

configBuilder.Build();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder
    .Services
    .AddSingleton(
        builder
            .GetAndValidateTypedSection(nameof(Settings), new SettingsValidator())
    );

builder
    .Services
    .AddSingleton(
        builder
            .GetTypedSection<ConfigKeys>(nameof(ConfigKeys))
    );

var memoryCache = new MemoryCache(new MemoryCacheOptions());

builder.Services.AddSingleton(memoryCache);
builder.Services.RegisterServicesInAssembly(typeof(ValidationService));
builder.Services.RegisterServicesInAssembly(typeof(EmailService));

builder.Services.AddValidatorsFromAssemblyContaining<SettingsValidator>();

var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

#pragma warning disable ASP0014
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
#pragma warning restore ASP0014


app.MapFallbackToFile("index.html");

app.Run();
