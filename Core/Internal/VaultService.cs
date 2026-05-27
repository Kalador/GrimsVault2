using Microsoft.Extensions.Configuration;
using Vault.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Vault.Core;

/// <summary>
/// The Central Core Service. All calls to the VaultService are routed through this class.
/// It is responsible for managing the lifecycle of all features and services, and for
/// providing a single point of access to the Vault's functionality.
/// </summary>
public class VaultService
{
    private readonly IDataHandler _data;

    /// <summary>
    /// Provides access to the VaultCache. Callers can access feature-specific caches through this property.
    /// The VaultCache is a singleton that is shared across all features and services.
    /// </summary>
    public VaultCache Cache { get; } = new VaultCache();

    /// <summary>
    /// Provides access to the Core Email Sender. Features can call vault.Email.SendHtmlEmailAsync(...)
    /// to send HTML emails using the configured SMTP credentials.
    /// </summary>
    public EmailSender Email { get; }

    /// <summary>
    /// Checks the status of the VaultService and returns a simple string indicating that it is operational.
    /// </summary>
    public string Status() => "VaultService OK";

    // ------------------------------------------------------------
    // CONSTRUCTOR
    // ------------------------------------------------------------
    public VaultService(IDataHandler data, IConfiguration config)
    {
        _data = data;

        var smtpUser = config["Email:SmtpUser"] ?? "";
        var smtpPass = config["Email:SmtpPass"] ?? "";
        var smtpHost = config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.TryParse(config["Email:SmtpPort"], out var p) ? p : 587;

        var recipients = config
            .GetSection("Email:Recipients")
            .Get<string[]>()
            ?? [];

        Email = new EmailSender(smtpUser, smtpPass, smtpHost, smtpPort, recipients);
    }

    /// <summary>
    /// Retrieves VaultItems from the data handler.
    /// Optional <see cref="QueryDefinition"/> applies AND filters.
    /// </summary>
    public async Task<List<VaultItem>> GetItems(
        QueryDefinition? query = null,
        int? pageSize = null)
    {
        return await _data.GetItemsAsync(query, pageSize);
    }

    /// <summary>
    /// Retrieves a VaultItem from the data handler.
    /// </summary>
    public async Task<VaultItem?> GetItem(string id)
    {
        return await _data.GetItemAsync(id);
    
    }

    /// <summary>
    /// Updates a single VaultItem in the backing store.
    /// </summary>
    public async Task UpdateItem(VaultItem item)
    {
        await _data.    UpdateItemAsync(item);
    }

    /// <summary>
    /// Updates multiple VaultItems in the backing store.
    /// </summary>
    public async Task UpdateItems(IEnumerable<VaultItem> items)
    {
        await _data.UpdateItemsAsync(items);
    }


    /// <summary>
    /// Retrieves VaultStores from the data handler.
    /// Optional <see cref="QueryDefinition"/> applies AND filters.
    /// </summary>
    public async Task<List<VaultStore>> GetStores(
        QueryDefinition? query = null,
        int? pageSize = null)
    {
        return await _data.GetStoresAsync(query, pageSize);
    }

}
