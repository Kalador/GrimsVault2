using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.FileProviders;
using Vault.Core;
using Vault.Data;
using Vault.Data.Notion;

var builder = WebApplication.CreateBuilder(args);

//Custom Configuration for the application
//------------------------------------------------------------------------------------------------------------------
//This is the place to register a data providers. The provider should implement IDataHandler and be registered here.
//Only one data provider should be registered at a time.
builder.Services.AddNotion();
//------------------------------------------------------------------------------------------------------------------

// MVC and Razor view configuration
// Enables modular view discovery so each feature can contain its own Views folder.
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Features/{1}/Views/{0}.cshtml");
    options.ViewLocationFormats.Add("/Features/{1}/Views/Shared/{0}.cshtml");
    options.ViewLocationFormats.Add("/Core/Views/{0}.cshtml");
    options.ViewLocationFormats.Add("/Core/Views/Shared/{0}.cshtml");
});

// Load root configuration files
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Load configuration from each feature folder
var featureRoot = Path.Combine(builder.Environment.ContentRootPath, "Features");
if (Directory.Exists(featureRoot))
{
    foreach (var featureDir in Directory.GetDirectories(featureRoot))
    {
        var configPath = Path.Combine(featureDir, "appsettings.json");
        var secretPath = Path.Combine(featureDir, "appsettings.Secrets.json");
        if (File.Exists(configPath)) builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
        if (File.Exists(secretPath)) builder.Configuration.AddJsonFile(secretPath, optional: true, reloadOnChange: true);
    }
}

// Load configuration from each data provider folder
var dataRoot = Path.Combine(builder.Environment.ContentRootPath, "Data");
if (Directory.Exists(dataRoot))
{
    foreach (var dataDir in Directory.GetDirectories(dataRoot))
    {
        var configPath = Path.Combine(dataDir, "appsettings.json");
        var secretPath = Path.Combine(dataDir, "appsettings.Secrets.json");
        if (File.Exists(configPath)) builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
        if (File.Exists(secretPath)) builder.Configuration.AddJsonFile(secretPath, optional: true, reloadOnChange: true);
    }
}

// Register modular features
var featureTypes = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .Where(t => typeof(IVaultFeature).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

foreach (var type in featureTypes)
{
    builder.Services.AddScoped(typeof(IVaultFeature), type);
    builder.Services.AddScoped(type);
}


    //
    // Core application services
    //
    builder.Services.AddScoped<VaultService>();

//
// Build and configure the request pipeline
//
var app = builder.Build();
app.UseStaticFiles();

// Expose wwwroot folders in any found ViewMode folders:
var viewModesRoot = Path.Combine(builder.Environment.ContentRootPath, "ViewModes");
if (Directory.Exists(viewModesRoot))
{
    foreach (var dir in Directory.GetDirectories(viewModesRoot))
    {
        var wwwroot = Path.Combine(dir, "wwwroot");
        if (Directory.Exists(wwwroot))
        {
            var folderName = Path.GetFileName(dir); 
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(wwwroot),
                RequestPath = $"/ViewModes/{folderName}"

            });
        }
    }
}



app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Vault}/{action=Index}/{id?}"
);

app.Run();
