using Microsoft.AspNetCore.Mvc.Razor;
using Vault.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Add("/Features/{1}/Views/{0}.cshtml");
    options.ViewLocationFormats.Add("/Features/{1}/Views/Shared/{0}.cshtml");
});


// Register VaultService
builder.Services.AddScoped<VaultService>();

// Dynamically register all IVaultFeature implementations
var assemblies = AppDomain.CurrentDomain.GetAssemblies();

foreach (var assembly in assemblies)
{
    // 1. Register all feature services (IVaultFeature)
    var featureServices = assembly.GetTypes()
        .Where(t => typeof(IVaultFeature).IsAssignableFrom(t))
        .Where(t => t.IsClass && !t.IsAbstract);

    foreach (var type in featureServices)
    {
        // Register as the feature service interface
        builder.Services.AddScoped(typeof(IVaultFeature), type);

        // ALSO register as itself so controllers can inject it
        builder.Services.AddScoped(type);
    }

    // 2. Register all metadata providers (IVaultFeatureMetaData)
    var metadataTypes = assembly.GetTypes()
        .Where(t => typeof(IVaultFeatureMetaData).IsAssignableFrom(t))
        .Where(t => t.IsClass && !t.IsAbstract);

    foreach (var type in metadataTypes)
    {
        builder.Services.AddScoped(typeof(IVaultFeatureMetaData), type);

        // If metadata is on the same class as the service, this is harmless
        builder.Services.AddScoped(type);
    }
}


var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Vault}/{action=Index}/{id?}");

app.Run();
