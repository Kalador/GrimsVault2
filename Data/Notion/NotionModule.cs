namespace Vault.Data.Notion;

/// <summary>
/// Class to register Notion services and dependencies in the DI container.
/// </summary>
public static class NotionModule
{
    public static IServiceCollection AddNotion(this IServiceCollection services)
    {
        services.AddHttpClient<NotionClient>(); 
        services.AddScoped<IDataHandler, NotionDataHandler>();
        return services;
    }
}