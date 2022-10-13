using OsborneSupremacy.Extensions.Net.DependencyInjection;
using OsborneSupremacy.Extensions.AspNet;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");

configBuilder.AddUserSecrets<Program>(true);

var config = configBuilder.Build();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddSwaggerDocument(config =>
{
    config.PostProcess = document =>
    {
        document.Info.Version = "v1";
        document.Info.Title = "Names Out Of A Hat";
        document.Info.Description = "A web application to facilitate a \"names out of hat\" type gift exchange, written with Blazor.";
        document.Info.Contact = new NSwag.OpenApiContact
        {
            Name = "Ben Osborne",
            Email = "ben@osbornesupremacy.com",
            Url = "https://github.com/OsborneSupremacy"
        };
    };
});

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
builder.Services.RegisterServicesInAssembly(typeof(SendGridEmailService));

var app = builder.Build();

app.UseStaticFiles();
app.UseOpenApi();

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

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapFallbackToFile("index.html");

app.Run();
