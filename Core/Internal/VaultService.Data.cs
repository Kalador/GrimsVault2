namespace Vault.Core;

//Partial Class for Notion API Code. Seperated for clarity and maintainability.
public partial class VaultService
{

    public async Task<List<VaultItem>> GetItems(QueryDefinition? query = null, int? pageSize = null)
    {
        return await data.GetItemsAsync(query, pageSize);
    }
}