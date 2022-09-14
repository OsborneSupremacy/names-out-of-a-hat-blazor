var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json");

configBuilder.AddUserSecrets<Program>(true);

// If you have a an Azure App config, when running locally, can store it in launchSettings.json
// On Azure, store it in application settings
var azureConfigConnectionString = Environment
    .GetEnvironmentVariable("AZURE_CONFIG_CONNECTIONSTRING") ?? string.Empty;

if (!string.IsNullOrWhiteSpace(azureConfigConnectionString))
    configBuilder.AddAzureAppConfiguration(azureConfigConnectionString);

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

builder.Services.Configure<Settings>(config.GetSection(nameof(Settings)));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<Settings>>().Value);

builder.Services.Configure<ConfigKeys>(
    config.GetSection(nameof(ConfigKeys)));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<ConfigKeys>>().Value);

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
