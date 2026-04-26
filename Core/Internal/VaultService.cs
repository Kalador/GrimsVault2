using Vault.Data;

namespace Vault.Core;

public partial class VaultService(IDataHandler data)
{
    private readonly IDataHandler _data = data;

    public string Status() => "VaultService OK";

    public async Task<List<VaultItem>> GetItems(int? pageSize = null)
    {
        return await _data.GetItemsAsync(pageSize);
    }

}
